using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Couchbase.Core.Diagnostics.Metrics;
using Couchbase.Core.IO.Operations;
using Couchbase.Core.Logging;
using Couchbase.Utils;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Couchbase.Core.IO.Connections.Channels
{
    /// <summary>
    /// Connection pool based on queuing operations via the TPL data flows library.
    /// </summary>
    internal sealed partial class ChannelConnectionPool : ConnectionPoolBase
    {
        private static readonly Random _random = new();

        private readonly IConnectionPoolScaleController _scaleController;
        private readonly IRedactor _redactor;
        private readonly ILogger<ChannelConnectionPool> _logger;
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _lock = new(1);
        private readonly List<ChannelConnectionProcessor> _connections = new();

        private readonly Channel<ChannelQueueItem> _sendQueue;

        private bool _initialized;
        private bool _disposed;

        /// <inheritdoc />
        public override int Size => _connections.Count;

        /// <inheritdoc />
        public override int MinimumSize { get; set; }

        /// <inheritdoc />
        public override int MaximumSize { get; set; }

        /// <inheritdoc />
        public override int PendingSends => _sendQueue.Reader.Count;

        /// <summary>
        /// Creates a new ChannelConnectionPool.
        /// </summary>
        /// <param name="connectionInitializer">Handler for initializing new connections.</param>
        /// <param name="connectionFactory">Factory for creating new connections.</param>
        /// <param name="scaleController">Scale controller.</param>
        /// <param name="redactor">Log redactor.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="sendQueueCapacity">The maximum number of items the channel will store.
        /// Defaults to 1024 and is configurable via <see cref="Couchbase.ClusterOptions.KvSendQueueCapacity"/></param>
        public ChannelConnectionPool(IConnectionInitializer connectionInitializer, IConnectionFactory connectionFactory,
            IConnectionPoolScaleController scaleController, IRedactor redactor, ILogger<ChannelConnectionPool> logger, int sendQueueCapacity) :
            this(connectionInitializer, connectionFactory, scaleController, redactor, logger, CreateDefaultChannel(sendQueueCapacity))
        {
        }

        /// <summary>
        /// Creates a new ChannelConnectionPool.
        /// </summary>
        /// <param name="connectionInitializer">Handler for initializing new connections.</param>
        /// <param name="connectionFactory">Factory for creating new connections.</param>
        /// <param name="scaleController">Scale controller.</param>
        /// <param name="redactor">Log redactor.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="channel">Channel queue.</param>
        internal ChannelConnectionPool(IConnectionInitializer connectionInitializer, IConnectionFactory connectionFactory,
            IConnectionPoolScaleController scaleController, IRedactor redactor, ILogger<ChannelConnectionPool> logger, Channel<ChannelQueueItem> channel)
            : base(connectionInitializer, connectionFactory, logger)
        {
            _scaleController = scaleController ?? throw new ArgumentNullException(nameof(scaleController));
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            MinimumSize = 2;
            MaximumSize = 5;

            _sendQueue = channel;

            TrackConnectionPool(this);
        }

        private static Channel<ChannelQueueItem> CreateDefaultChannel(int sendQueueCapacity)
        {
            return Channel.CreateBounded<ChannelQueueItem>(new BoundedChannelOptions(sendQueueCapacity)
            {
                AllowSynchronousContinuations = true
            });
        }

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            await AddConnectionsAsync(MinimumSize, cancellationToken).ConfigureAwait(false);

            _scaleController.Start(this);

            InitializedConnectionPool(_redactor.SystemData(EndPoint), MinimumSize);

            _initialized = true;
            _disposed = false;
        }


        /// <inheritdoc />
        public override Task SendAsync(IOperation operation, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            // Note: Because synchronous continuations are enabled, The ChannelConnectionProcessor's work to process items
            // from the queue may take place synchronously on this thread and momentarily block the return of this method.
            // However, this gives us the performance benefit of not queuing the send work on the thread pool. The time
            // involved is the time to serialize an operation to a buffer and send it to the socket (but not the actual
            // send over the network). Note, however, that the operation serialized may not be the operation being sent here,
            // it will be the next operation in the queue. So a large operation may block very briefly, and later a small
            // operation may block longer.
            if (!_sendQueue.Writer.TryWrite(new ChannelQueueItem(operation, cancellationToken)))
            {
                MetricTracker.KeyValue.TrackSendQueueFull();
                ThrowHelper.ThrowSendQueueFullException();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task<bool> TrySendImmediatelyAsync(IOperation op, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            IConnection? connection = GetConnectionsCore().RandomOrDefault();
            if (connection is null)
            {
                // Fallback to sending via the queue if no connections are available
                await SendAsync(op, cancellationToken).ConfigureAwait(false);
                return false;
            }

            // Create queue item and send it so that we replicate all behaviors of the queue item.
            // However, don't capture the ExecutionContext since we're not queueing on the pool and therefore
            // don't need to restore the context. It's captured instead when awaiting.
            var queueItem = new ChannelQueueItem(op, cancellationToken, captureContext: false);
            await queueItem.SendAsync(connection).ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc />
        public override IEnumerable<IConnection> GetConnections()
        {
            EnsureNotDisposed();

            return GetConnectionsCore().ToList();
        }

        private IEnumerable<IConnection> GetConnectionsCore() =>
            _connections
                .Where(static p => !p.IsComplete)
                .Select(static p => p.Connection);

        /// <inheritdoc />
        public override async Task ScaleAsync(int delta)
        {
            if (delta > 0)
            {
                var growBy = Math.Min(delta, MaximumSize - Size);
                if (growBy > 0)
                {
                    await AddConnectionsAsync(growBy, _cts.Token).ConfigureAwait(false);
                }
            }
            else if (delta < 0)
            {
                var shrinkBy = Math.Min(-delta, Size - MinimumSize);
                if (shrinkBy > 0)
                {
                    // Select connections to shrink, longest inactive first
                    var toShrink = _connections
                        .OrderByDescending(p => p.Connection.IdleTime)
                        .Take(shrinkBy)
                        .ToList();

                    // Stop all connections from receiving new sends, and wait for in flight sends
                    // to complete, in parallel
                    var completionTasks = toShrink
                        .Select(p => p.CompleteAsync());
                    await Task.WhenAll(completionTasks).ConfigureAwait(false);

                    // Remove from _connections
                    foreach (var p in toShrink)
                    {
                        _connections.Remove(p);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async ValueTask<IAsyncDisposable> FreezePoolAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            return new FreezeDisposable(this);
        }

        private void EnsureNotDisposed()
        {
            if (_cts.IsCancellationRequested)
            {
                //Were not throwing an ODE because we want a more specific exception that reuse the retry logic in the RetryOrchestrator
                ThrowHelper.ThrowSocketNotAvailableException(nameof(ChannelConnectionPool));
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (_disposed)
            {
                LogAlreadyDisposedConnectionPool(EndPoint);
                return;
            }

            LogDisposeConnectionPool(EndPoint);
            _disposed = true;
            _scaleController.Dispose();
            _cts.Cancel(false);

            // Take out a lock to prevent more connections from opening while we're disposing
            // Don't need to release
            _lock.Wait();
            try
            {
                // Complete any queued commands
                _sendQueue.Writer.TryComplete();

                // Dispose of the connections. The ChannelConnectionProcessor should do this,
                // but we'd like to be more proactive in this case.
                foreach (var connection in _connections)
                {
                    connection.Connection.Dispose();
                }

                _connections.Clear();
            }
            finally
            {
                _lock.Dispose();
                _cts.Dispose();
            }
        }

        #region Connection Management

        /// <summary>
        /// Adds a certain number of connections to the pool. Assumes that the pool is already locked.
        /// </summary>
        /// <param name="count">Number of connections to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// This method will fail if the total number of requested connections could not be added.
        /// However, it may have partially succeeded, some connections may have been added.
        /// </remarks>
        private Task AddConnectionsAsync(int count, CancellationToken cancellationToken = default)
        {
            if (count <= 0)
            {
                return Task.CompletedTask;
            }

            async Task StartConnection(int _)
            {
                // Create and initialize a new connection
                var connection = await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

                if (connection.IsDead)
                {
                    LogConnectionToEndpointError(EndPoint);
                    return;
                }

                LogConnectionToEndpointSuccess(EndPoint);

                var processor = new ChannelConnectionProcessor(connection, this, _sendQueue.Reader, _logger).Start();

                lock (_connections)
                {
                    // As each connection is successful, add it to our list of connections
                    // This way if 4 succeed and 1 fails, the 4 that succeed are still up and available
                    // We need an additional lock here because _connections.Add might get called
                    // simultaneously as each connection is successfully started, but this is a different
                    // lock from the preexisting lock on the overall pool using _lock.

                    _connections.Add(processor);
                }
            }

            // Startup connections up to the minimum pool size in parallel
            var tasks =
                Enumerable.Range(1, count)
                    .Select(StartConnection)
                    .ToList();

            // Wait for all connections to be started
            return Task.WhenAll(tasks);
        }

        public async ValueTask RemoveConnectionAsync(ChannelConnectionProcessor connection)
        {
            await _lock.WaitAsync(_cts.Token).ConfigureAwait(false);
            try
            {
                _connections.Remove(connection);

                if (_connections.Count < MinimumSize)
                {
                    await AddConnectionsAsync(MinimumSize - _connections.Count).ConfigureAwait(false);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        #endregion

        private class FreezeDisposable : IAsyncDisposable
        {
            private readonly ChannelConnectionPool _connectionPool;

            public FreezeDisposable(ChannelConnectionPool connectionPool)
            {
                _connectionPool = connectionPool;
            }

            public ValueTask DisposeAsync()
            {
                _connectionPool._lock.Release();

                return default;
            }
        }

        #region Logging

        [LoggerMessage(1, LogLevel.Debug, "Connection pool for {endpoint} initialized with {size} connections.")]
        private partial void InitializedConnectionPool(object endpoint, int size);

        [LoggerMessage(LoggingEvents.ChannelConnectionEvent, LogLevel.Debug, "Connection for {endpoint} has been started.")]
        private partial void LogConnectionToEndpointSuccess(HostEndpointWithPort endpoint);

        [LoggerMessage(100, LogLevel.Debug, "Connection for {endpoint} could not be started.")]
        private partial void LogConnectionToEndpointError(HostEndpointWithPort endpoint);

        [LoggerMessage(101, LogLevel.Debug, "Disposing pool for {endpoint}.")]
        private partial void LogDisposeConnectionPool(HostEndpointWithPort endpoint);

        [LoggerMessage(102, LogLevel.Debug, "Disposing pool for {endpoint} that was already disposed")]
        private partial void LogAlreadyDisposedConnectionPool(HostEndpointWithPort endpoint);

        #endregion
    }
}


/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2021 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

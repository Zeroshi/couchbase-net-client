using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.Diagnostics.Metrics;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.IO.Connections;
using Couchbase.Core.IO.Operations;
using Couchbase.Core.IO.Operations.Errors;
using Couchbase.Core.IO.Transcoders;
using Couchbase.Core.Retry;
using Couchbase.Utils;

#pragma warning disable CS8632

namespace Couchbase.UnitTests.Core.Diagnostics.Tracing.Fakes
{
    internal class FakeOperation : IOperation
    {
        private static Random _random = new Random();
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private System.Diagnostics.Stopwatch _operationAge = System.Diagnostics.Stopwatch.StartNew();
        public TimeSpan Elapsed => _operationAge.Elapsed;

        public Couchbase.Core.Diagnostics.Tracing.IRequestSpan Span2 { get; set; }
        public uint Attempts { get; set; }
        public bool Idempotent { get; }
        public List<RetryReason> RetryReasons { get; set; }
        public IRetryStrategy RetryStrategy { get; set; }
        public TimeSpan Timeout { get; set; }
        public CancellationToken Token
        {
            get => TokenPair;
            set => throw new NotImplementedException();
        }

        public CancellationTokenPair TokenPair { get; set; }
        public string? ClientContextId { get; set; }
        public string? Statement { get; set; }

        public bool PreserveTtl { get; }
        public OpCode OpCode { get; }
        public string? BucketName { get; }
        public string? SName { get; }
        public string? CName { get; }
        public uint? Cid { get; set; }
        public string Key { get; }
        public byte[] EncodedKey { get; }
        public bool RequiresVBucketId { get; }
        public short? VBucketId { get; set; }
        public short? ReplicaIdx { get; }
        public uint Opaque { get; }
        public ulong Cas { get; }
        public OperationHeader Header { get; }
        public IRequestSpan Span { get; }
        public IValueRecorder Recorder { get; set; }
        public bool HasDurability { get; }
        public bool IsReadOnly { get; }
        public bool IsSent { get; }
        public ValueTask<ResponseStatus> Completed { get; }
        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(IConnection connection, CancellationToken cancellationToken = default)
        {
            connection.AddTags(Span2);

            var dispatch = _random.Next(200, 1000);
            using var dispatchSpan = Span2.EncodingSpan();
            //await Task.Delay(dispatch);

            using var encodingSpan = dispatchSpan.DispatchSpan(this);
            var encoding = _random.Next(200, 1000);
            return Task.CompletedTask;
            //await Task.Delay(encoding);
        }

        public bool TrySetCanceled(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public bool TrySetException(Exception ex)
        {
            throw new NotImplementedException();
        }

        public void HandleOperationCompleted(in SlicedMemoryOwner<byte> data)
        {
            throw new NotImplementedException();
        }

        public SlicedMemoryOwner<byte> ExtractBody()
        {
            throw new NotImplementedException();
        }

        public BucketConfig? ReadConfig(ITypeTranscoder transcoder)
        {
            throw new NotImplementedException();
        }

        public bool WasNmvb()
        {
            throw new NotImplementedException();
        }

        public long? LastServerDuration { get; }

        public string LastDispatchedFrom => throw new NotImplementedException();

        public string LastDispatchedTo => throw new NotImplementedException();
        public bool IsCompleted { get; }
        public ErrorCode LastErrorCode { get; set; }
        public bool RetryNow()
        {
            throw new NotImplementedException();
        }

        public string LastErrorMessage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool PreferReturns { get; }
        public bool CanStream => throw new NotImplementedException();

        public void LogOrphaned()
        {
            throw new NotImplementedException();
        }

        public void StopRecording()
        {
            throw new NotImplementedException();
        }
    }
}

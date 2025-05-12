using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.Diagnostics.Metrics.AppTelemetry;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.Analytics;
using Couchbase.Core.IO.HTTP;
using Couchbase.Core.IO.Serializers;
using Couchbase.Core.Logging;
using Couchbase.Core.Utils;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Couchbase.Analytics
{
    internal class AnalyticsClient : HttpServiceBase, IAnalyticsClient
    {
        internal const string AnalyticsRequiresUnreferencedMembersWarning =
            "Couchbase Analytics might require types that cannot be statically analyzed. Make sure all required types are preserved.";
        internal const string AnalyticsRequiresDynamicCodeWarning =
            "Couchbase Analytics might require types that cannot be statically analyzed and might need runtime code generation. Do not use for native AOT applications.";

        private readonly IServiceUriProvider _serviceUriProvider;
        private readonly ITypeSerializer _typeSerializer;
        private readonly ILogger<AnalyticsClient> _logger;
        private readonly IRequestTracer _tracer;
        private readonly IAppTelemetryCollector _appTelemetryCollector;
        internal const string AnalyticsPriorityHeaderName = "Analytics-Priority";

        [RequiresUnreferencedCode(AnalyticsRequiresUnreferencedMembersWarning)]
        [RequiresDynamicCode(AnalyticsRequiresDynamicCodeWarning)]
        public AnalyticsClient(
            ICouchbaseHttpClientFactory httpClientFactory,
            IServiceUriProvider serviceUriProvider,
            ITypeSerializer typeSerializer,
            ILogger<AnalyticsClient> logger,
            IRequestTracer tracer,
            IAppTelemetryCollector appTelemetryCollector)
            : base(httpClientFactory)
        {
            _serviceUriProvider = serviceUriProvider ?? throw new ArgumentNullException(nameof(serviceUriProvider));
            _typeSerializer = typeSerializer ?? throw new ArgumentNullException(nameof(typeSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tracer = tracer;
            _appTelemetryCollector = appTelemetryCollector;
        }

        /// <inheritdoc />
        [RequiresUnreferencedCode(AnalyticsRequiresUnreferencedMembersWarning)]
        [RequiresDynamicCode(AnalyticsRequiresDynamicCodeWarning)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2046",
            Justification = "This type may not be constructed without encountering a warning.")]
        [UnconditionalSuppressMessage("AOT", "IL3051",
            Justification = "This type may not be constructed without encountering a warning.")]
        public async Task<IAnalyticsResult<T>> QueryAsync<T>(string statement, AnalyticsOptions options)
        {
            using var rootSpan = RootSpan(OuterRequestSpans.ServiceSpan.AnalyticsQuery, options)
                .WithOperationId(options)
                .WithLocalAddress();

            // try get Analytics node
            var analyticsNode = _serviceUriProvider.GetRandomAnalyticsNode();
            var analyticsUri = analyticsNode.AnalyticsUri;
            var requestStopwatch = _appTelemetryCollector.StartNewLightweightStopwatch();
            TimeSpan? operationElapsed;

            rootSpan.WithRemoteAddress(analyticsUri);

            _logger.LogDebug("Sending analytics query with a context id {contextId} to server {searchUri}",
                options.ClientContextIdValue, analyticsUri);

            using var encodingSpan = rootSpan.EncodingSpan();

            AnalyticsResultBase<T> result;
            var body = options.GetFormValuesAsJson(statement);

            using (var content = new StringContent(body, Encoding.UTF8, MediaType.Json))
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, analyticsUri)
                    {
                        Content = content
                    };

                    if (options.PriorityValue != 0)
                    {
                        request.Headers.Add(AnalyticsPriorityHeaderName, new[] {options.PriorityValue.ToStringInvariant()});
                    }

                    encodingSpan.Dispose();
                    using var dispatchSpan = rootSpan.DispatchSpan(options);
                    var httpClient = CreateHttpClient(options.TimeoutValue);
                    try
                    {
                        requestStopwatch?.Restart();
                        var response = await httpClient.SendAsync(request, HttpClientFactory.DefaultCompletionOption, options.Token)
                            .ConfigureAwait(false);
                        operationElapsed = requestStopwatch?.Elapsed;
                        dispatchSpan.Dispose();

                        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                        if (_typeSerializer is IStreamingTypeDeserializer streamingTypeDeserializer)
                        {
                            result = new StreamingAnalyticsResult<T>(stream, streamingTypeDeserializer, ownedForCleanup: httpClient)
                            {
                                HttpStatusCode = response.StatusCode
                            };
                        }
                        else
                        {
                            result = new BlockAnalyticsResult<T>(stream, _typeSerializer, ownedForCleanup: httpClient)
                            {
                                HttpStatusCode = response.StatusCode
                            };
                        }

                        await result.InitializeAsync(options.Token).ConfigureAwait(false);

                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            var context = new AnalyticsErrorContext
                            {
                                ClientContextId = options.ClientContextIdValue,
                                HttpStatus = response.StatusCode,
                                Statement = statement,
                                Parameters = options.GetParametersAsJson(),
                                Errors = result.Errors
                            };

                            if (result.ShouldRetry())
                            {
                                result.NoRetryException = CreateExceptionForError(result, context, true);
                                UpdateLastActivity();
                                return result;
                            }

                            CouchbaseException? ex = CreateExceptionForError(result, context, false);
                            if (ex != null) { throw ex; }
                        }

                        _appTelemetryCollector.IncrementMetrics(
                            operationElapsed,
                            analyticsNode.NodesAdapter.CanonicalHostname,
                            analyticsNode.NodesAdapter.AlternateHostname,
                            analyticsNode.NodeUuid,
                            AppTelemetryServiceType.Analytics,
                            AppTelemetryCounterType.Total);
                    }
                    catch
                    {
                        // Ensure the HttpClient is disposed on an exception. On success scenarios it is disposed when the caller
                        // disposes of the returned IAnalyticsResult. HttpClient is not simply disposed in every case because doing so
                        // causes exceptions in .NET 4 when using HttpCompletionOption.ResponseHeadersRead because it closes the socket
                        // before the body is fully read.
                        httpClient.Dispose();
                        throw;
                    }
                }
                catch (OperationCanceledException e)
                {
                    operationElapsed = requestStopwatch?.Elapsed;
                    //treat as an orphaned response
                    rootSpan.LogOrphaned();

                    _appTelemetryCollector.IncrementMetrics(
                        operationElapsed,
                        analyticsNode.NodesAdapter.CanonicalHostname,
                        analyticsNode.NodesAdapter.AlternateHostname,
                        analyticsNode.NodeUuid,
                        AppTelemetryServiceType.Analytics,
                        AppTelemetryCounterType.TimedOut);

                    var context = new AnalyticsErrorContext
                    {
                        ClientContextId = options.ClientContextIdValue,
                        Statement = statement,
                        Parameters = options.GetParametersAsJson()
                    };

                    _logger.LogDebug(LoggingEvents.AnalyticsEvent, e, "Analytics request timeout.");
                    if (options.ReadonlyValue)
                    {
                        throw new UnambiguousTimeoutException("The query was timed out via the Token.", e)
                        {
                            Context = context
                        };
                    }

                    throw new AmbiguousTimeoutException("The query was timed out via the Token.", e)
                    {
                        Context = context
                    };
                }
                catch (HttpRequestException e)
                {
                    operationElapsed = requestStopwatch?.Elapsed;
                    //treat as an orphaned response
                    rootSpan.LogOrphaned();

                    _appTelemetryCollector.IncrementMetrics(
                        operationElapsed,
                        analyticsNode.NodesAdapter.CanonicalHostname,
                        analyticsNode.NodesAdapter.AlternateHostname,
                        analyticsNode.NodeUuid,
                        AppTelemetryServiceType.Analytics,
                        AppTelemetryCounterType.Canceled);

                    var context = new AnalyticsErrorContext
                    {
                        ClientContextId = options.ClientContextIdValue,
                        Statement = statement,
                        Parameters = options.GetParametersAsJson()
                    };

                    _logger.LogDebug(LoggingEvents.AnalyticsEvent, e, "Analytics request cancelled.");
                    throw new RequestCanceledException("The query was canceled.", e)
                    {
                        Context = context
                    };
                }
            }

            UpdateLastActivity();
            return result;
        }

        /// <summary>
        /// Create the appropriate Exception for an error context
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="result">Result</param>
        /// <param name="context">Error context</param>
        /// <param name="couchbaseExceptionFallback">Flag on whether a fallback CouchbaseException should be created</param>
        /// <returns>Nullable Exception</returns>
        private CouchbaseException? CreateExceptionForError<T>(AnalyticsResultBase<T> result, AnalyticsErrorContext context, bool couchbaseExceptionFallback)
        {
            if (result.LinkNotFound()) return new LinkNotFoundException(context);
            if (result.DataverseExists()) return new DataverseExistsException(context);
            if (result.DatasetExists()) return new DatasetExistsException();
            if (result.DataverseNotFound()) return new DataverseNotFoundException(context);
            if (result.DataSetNotFound()) return new DatasetNotFoundException(context);
            if (result.JobQueueFull()) return new JobQueueFullException(context);
            if (result.InternalServerFailure()) return new InternalServerFailureException(context);
            if (result.AuthenticationFailure()) return new AuthenticationFailureException(context);
            if (result.TemporaryFailure()) return new TemporaryFailureException(context);
            if (result.IndexNotFound()) return new IndexNotFoundException(context);
            if (result.IndexExists()) return new IndexExistsException(context);
            if (result.ParsingFailure()) return new ParsingFailureException(context);
            if (result.CompilationFailure()) return new CompilationFailureException(context);

            if (couchbaseExceptionFallback)
            {
                return new CouchbaseException(context);
            }
            else
            {
                return null;
            }
        }

        #region tracing
        private IRequestSpan RootSpan(string operation, AnalyticsOptions options)
        {
            var span = _tracer.RequestSpan(operation, options.RequestSpanValue);
            if (span.CanWrite)
            {
                span.SetAttribute(OuterRequestSpans.Attributes.System.Key, OuterRequestSpans.Attributes.System.Value);
                span.SetAttribute(OuterRequestSpans.Attributes.Service, OuterRequestSpans.ServiceSpan.AnalyticsQuery);
                span.SetAttribute(OuterRequestSpans.Attributes.BucketName, options.BucketName!);
                span.SetAttribute(OuterRequestSpans.Attributes.ScopeName, options.ScopeName!);
                span.SetAttribute(OuterRequestSpans.Attributes.Operation, operation);
            }

            return span;
        }
        #endregion
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
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

#endregion

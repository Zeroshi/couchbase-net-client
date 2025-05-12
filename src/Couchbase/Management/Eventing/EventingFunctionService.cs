#nullable enable
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.IO.HTTP;
using Couchbase.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Couchbase.Management.Eventing
{
    /// <inheritdoc cref="IEventingFunctionService" />
    internal class EventingFunctionService : HttpServiceBase, IEventingFunctionService
    {
        private readonly IServiceUriProvider _serviceUriProvider;
        private readonly ILogger<EventingFunctionService> _logger;
        private readonly IRedactor _redactor;

        public EventingFunctionService(ICouchbaseHttpClientFactory httpClientFactory, IServiceUriProvider serviceUriProvider,
            ILogger<EventingFunctionService> logger, IRedactor redactor)
            : base(httpClientFactory)
        {
            _serviceUriProvider = serviceUriProvider;
            _logger = logger;
            _redactor = redactor;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetAsync(Uri requestUri, IRequestSpan parentSpan, IRequestSpan encodeSpan, CancellationToken token)
        {
            parentSpan.WithRemoteAddress(requestUri);

            encodeSpan.Dispose();
            using var dispatchSpan = parentSpan.DispatchSpan();
            var httpClient = CreateHttpClient();
            return httpClient.GetAsync(requestUri, token);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> PostAsync(Uri requestUri, IRequestSpan parentSpan, IRequestSpan encodeSpan, CancellationToken token, EventingFunction? eventingFunction = null, EventingFunctionKeyspace? managementScope = null)
        {
            parentSpan.WithRemoteAddress(requestUri);

            var content = eventingFunction != null ?
                new StringContent(eventingFunction.ToJson(managementScope)) :
                new StringContent(string.Empty);

            encodeSpan.Dispose();
            using var dispatchSpan = parentSpan.DispatchSpan();
            var httpClient = CreateHttpClient();
            return httpClient.PostAsync(requestUri, content, token);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, IRequestSpan parentSpan, IRequestSpan encodeSpan, CancellationToken token)
        {
            parentSpan.WithRemoteAddress(requestUri);

            encodeSpan.Dispose();
            using var dispatchSpan = parentSpan.DispatchSpan();
            var httpClient = CreateHttpClient();
            return httpClient.DeleteAsync(requestUri, token);
        }
    }
}

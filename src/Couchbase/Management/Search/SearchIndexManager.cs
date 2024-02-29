using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.Exceptions;
using Couchbase.Core.IO.HTTP;
using Couchbase.Core.Logging;
using Couchbase.KeyValue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Couchbase.Management.Search
{
    internal class SearchIndexManager : ISearchIndexManager
    {
        private readonly IServiceUriProvider _serviceUriProvider;
        private readonly ICouchbaseHttpClientFactory _httpClientFactory;
        private readonly ILogger<SearchIndexManager> _logger;
        private readonly IRedactor _redactor;
        private readonly ClusterContext _context;

        // TODO:  need to be able to reference global config to AssertCap(ScopedSearchIndexes)
        public SearchIndexManager(IServiceUriProvider serviceUriProvider, ICouchbaseHttpClientFactory httpClientFactory,
            ILogger<SearchIndexManager> logger, IRedactor redactor, ClusterContext context)
        {
            _serviceUriProvider = serviceUriProvider ?? throw new ArgumentNullException(nameof(serviceUriProvider));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
            _context = context;
        }

        private Uri GetIndexUri(IScope?  scope, string? indexName = null)
        {
            var searchUri = _serviceUriProvider.GetRandomSearchUri();
            var path = "api/index";
            if (scope is not null)
            {
                _context.GlobalConfig?.AssertCap(BucketCapabilities.SCOPED_SEARCH_INDEX);
                path = $"api/bucket/{scope.Bucket.Name}/scope/{scope.Name}";
            }

            var builder = new UriBuilder(searchUri)
            {
                Path = path
            };

            if (!string.IsNullOrWhiteSpace(indexName))
            {
                builder.Path += $"/{indexName}";
            }

            return builder.Uri;
        }

        private Uri GetQueryControlUri(string indexName, bool allow, IScope? scope)
        {
            var baseUri = GetIndexUri(scope, indexName);
            var control = allow ? "allow" : "disallow";

            return new UriBuilder(baseUri)
            {
                Path = $"{baseUri.PathAndQuery}/queryControl/{control}"
            }.Uri;
        }

        private Uri GetFreezeControlUri(string indexName, bool freeze, IScope? scope)
        {
            var baseUri = GetIndexUri(scope, indexName);
            var control = freeze ? "freeze" : "unfreeze";

            return new UriBuilder(baseUri)
            {
                Path = $"{baseUri.PathAndQuery}/planFreezeControl/{control}"
            }.Uri;
        }

        private Uri GetIngestControlUri(string indexName, bool pause, IScope? scope)
        {
            var baseUri = GetIndexUri(scope, indexName);
            var control = pause ? "pause" : "resume";

            return new UriBuilder(baseUri)
            {
                Path = $"{baseUri.PathAndQuery}/ingestControl/{control}"
            }.Uri;
        }

        private Uri GetIndexedDocumentCountUri(string indexName, IScope? scope = null)
        {
            var baseUri = GetIndexUri(scope, indexName);
            return new UriBuilder(baseUri)
            {
                Path = $"{baseUri.PathAndQuery}/count"
            }.Uri;
        }

        public async Task AllowQueryingAsync(string indexName, AllowQueryingSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= AllowQueryingSearchIndexOptions.Default;
            var baseUri = GetQueryControlUri(indexName, true, scope);
            _logger.LogInformation("Trying to allow querying for index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PostAsync(baseUri, null!, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to allow querying for index with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task DisallowQueryingAsync(string indexName, DisallowQueryingSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= DisallowQueryingSearchIndexOptions.Default;
            var baseUri = GetQueryControlUri(indexName, false, scope);
            _logger.LogInformation("Trying to disallow querying for index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PostAsync(baseUri, null!, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to disallow querying for index with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task DropIndexAsync(string indexName, DropSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= DropSearchIndexOptions.Default;
            var baseUri = GetIndexUri(scope, indexName);
            _logger.LogInformation("Trying to drop index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.DeleteAsync(baseUri, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to drop index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task FreezePlanAsync(string indexName, FreezePlanSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= FreezePlanSearchIndexOptions.Default;
            var baseUri = GetFreezeControlUri(indexName, true, scope);
            _logger.LogInformation("Trying to freeze index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PostAsync(baseUri, null!, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to freeze index with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task<IEnumerable<SearchIndex>> GetAllIndexesAsync(GetAllSearchIndexesOptions? options = null, IScope? scope = null)
        {
            options ??= GetAllSearchIndexesOptions.Default;
            var baseUri = GetIndexUri(scope);
            _logger.LogInformation("Trying to get all indexes - {baseUri}", _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.GetAsync(baseUri,  options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri).ConfigureAwait(false);

                var json = JObject.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
                return json["indexDefs"]!["indexDefs"]!.ToObject<Dictionary<string, SearchIndex>>()!.Values;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to get all indexes - {baseUri}", _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task<SearchIndex> GetIndexAsync(string indexName, GetSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= GetSearchIndexOptions.Default;
            var baseUri = GetIndexUri(scope, indexName);
            _logger.LogInformation("Trying to get index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.GetAsync(baseUri,  options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);

                var json = JObject.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
                return json["indexDef"]!.ToObject<SearchIndex>()!;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to get index with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task<int> GetIndexedDocumentsCountAsync(string indexName, GetSearchIndexDocumentCountOptions? options = null, IScope? scope = null)
        {
            options ??= GetSearchIndexDocumentCountOptions.Default;
            var baseUri = GetIndexedDocumentCountUri(indexName);
            _logger.LogInformation("Trying to get index document count with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.GetAsync(baseUri,  options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);

                var responseBody = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jobj = JObject.Parse(responseBody);
                return jobj["count"]!.Value<int>();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to get index document count with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task PauseIngestAsync(string indexName, PauseIngestSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= PauseIngestSearchIndexOptions.Default;
            var baseUri = GetIngestControlUri(indexName, true, scope);
            _logger.LogInformation("Trying to pause ingest for index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PostAsync(baseUri, null!, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to pause ingest for index with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task ResumeIngestAsync(string indexName, ResumeIngestSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= ResumeIngestSearchIndexOptions.Default;
            var baseUri = GetIngestControlUri(indexName, false, scope);
            _logger.LogInformation("Trying to resume ingest for index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PostAsync(baseUri, null!, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to resume ingest for index with name {indexName} - {baseUri}",
                    _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task UnfreezePlanAsync(string indexName, UnfreezePlanSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= UnfreezePlanSearchIndexOptions.Default;
            var baseUri = GetFreezeControlUri(indexName, false, scope);
            _logger.LogInformation("Trying to unfreeze index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));

            try
            {
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PostAsync(baseUri, null!, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri, indexName).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to unfreeze index with name {indexName} - {baseUri}",
                _redactor.MetaData(indexName), _redactor.SystemData(baseUri));
                throw;
            }
        }

        public async Task UpsertIndexAsync(SearchIndex indexDefinition, UpsertSearchIndexOptions? options = null, IScope? scope = null)
        {
            options ??= UpsertSearchIndexOptions.Default;
            var baseUri = GetIndexUri(scope, indexDefinition.Name);
            _logger.LogInformation("Trying to upsert index with name {indexDefinition.Name} - {baseUri}",
                _redactor.MetaData(indexDefinition.Name), _redactor.SystemData(baseUri));

            try
            {
                var json = JsonConvert.SerializeObject(indexDefinition, Formatting.None);
                var content = new StringContent(json, Encoding.UTF8, MediaType.Json);
                using var httpClient = _httpClientFactory.Create();
                var result = await httpClient.PutAsync(baseUri, content, options.TokenValue).ConfigureAwait(false);

                //Handle any errors that may exist
                await CheckStatusAndThrowIfErrorsAsync(result, baseUri).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to upsert index with name {indexDefinition.Name} - {baseUri}",
                    _redactor.MetaData(indexDefinition.Name), _redactor.SystemData(baseUri));
                throw;
            }
        }

        private static async Task CheckStatusAndThrowIfErrorsAsync(HttpResponseMessage  result, Uri uri, string? indexName = default)
        {
            if (!result.IsSuccessStatusCode)
            {
                var body = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                var ctx = new ManagementErrorContext
                {
                    HttpStatus = result.StatusCode,
                    Message = body,
                    Statement = uri.ToString()
                };

                if (result.StatusCode == HttpStatusCode.NotFound && indexName != null)
                    throw new SearchIndexNotFound(indexName)
                    {
                        Context = ctx
                    };

                //Throw specific exception if a rate limiting exception is thrown.
                result.ThrowIfRateLimitingError(body, ctx);

                //Throw any other error cases
                result.ThrowOnError(ctx);
            }
        }
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

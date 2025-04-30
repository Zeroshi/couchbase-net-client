using System;
using System.IO;
using System.Net.Http;
using Couchbase.Core;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.Logging;
using Couchbase.Management.Collections;
using Couchbase.UnitTests.Helpers;
using Couchbase.UnitTests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.UnitTests.Management
{
    public class CollectionManagerTests
    {
        private readonly string ScopeName = "MyScope";
        private readonly string CollectionName = "MyCollection";
        private static readonly Uri BaseUri = new Uri("http://localhost:8094/");
        private static readonly string BucketName = "default";
        private readonly CollectionManager _collectionManager;

        public CollectionManagerTests()
        {
            using var handler = FakeHttpMessageHandler.Create(req => new HttpResponseMessage
            {
                Content = new StreamContent(new MemoryStream())
            });
            var httpClient = new HttpClient(handler);
            var httpClientFactory = new MockHttpClientFactory(httpClient);
            var logger = new Mock<ILogger<CollectionManager>>().Object;
            var redactor = new Mock<IRedactor>().Object;

            var serviceUriProviderMock = new Mock<IServiceUriProvider>();
            serviceUriProviderMock.Setup(x => x.GetRandomManagementUri()).Returns(BaseUri);
            var serviceProvider = serviceUriProviderMock.Object;
            var bucketConfig = new Mock<BucketConfig>().Object;

            _collectionManager = new CollectionManager(BucketName, bucketConfig, serviceProvider, httpClientFactory, logger, redactor);
        }

        #region Uris

        [Fact]
        public void When_NotConnected_CollectionManager_Throws_NodeUnavailableException()
        {
            var clusterContext = new ClusterContext();
            var serviceUriProvider = new ServiceUriProvider(clusterContext);
            Assert.Throws<ServiceNotAvailableException>(() => serviceUriProvider.GetRandomManagementUri());
        }

        [Fact]
        public void Test_Retrieve_Scope_And_Manifest_For_Bucket()
        {
            var expected = new Uri(BaseUri + $"pools/default/buckets/{BucketName}/scopes");

            var actual =  _collectionManager.GetUri(CollectionManager.RestApi.GetScopes(BucketName));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_Create_Scope()
        {
            var expected = new Uri(BaseUri + $"pools/default/buckets/{BucketName}/scopes");

            var actual = _collectionManager.GetUri(CollectionManager.RestApi.CreateScope(BucketName));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_Delete_Scope()
        {
            var expected = new Uri(BaseUri + $"pools/default/buckets/{BucketName}/scopes/{ScopeName}");

            var actual = _collectionManager.GetUri(CollectionManager.RestApi.DeleteScope(BucketName, ScopeName));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_Create_Collection()
        {
            var expected = new Uri(BaseUri +
                                   $"pools/default/buckets/{BucketName}/scopes/{ScopeName}/collections");

            var actual = _collectionManager.GetUri(CollectionManager.RestApi.CreateCollections(BucketName, ScopeName));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_Delete_Collection()
        {
            var expected = new Uri(BaseUri +
                                   $"pools/default/buckets/{BucketName}/scopes/{ScopeName}/collections/{CollectionName}");

            var actual = _collectionManager.GetUri(CollectionManager.RestApi.DeleteCollections(BucketName, ScopeName, CollectionName));

            Assert.Equal(expected, actual);
        }

        #endregion
    }
}

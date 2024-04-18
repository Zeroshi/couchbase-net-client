using System;
using Couchbase.Core;
using Couchbase.Core.Bootstrapping;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.Configuration.Server.Streaming;
using Couchbase.Core.DI;
using Couchbase.Core.Diagnostics.Metrics;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.IO.Operations;
using Couchbase.Core.Logging;
using Couchbase.Core.Retry;
using Couchbase.Management.Buckets;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.UnitTests.Core.DI
{
    public class BucketFactoryTests
    {
        #region Create

        [Theory]
        [InlineData(BucketType.Couchbase, typeof(CouchbaseBucket))]
        [InlineData(BucketType.Ephemeral, typeof(CouchbaseBucket))]
        [InlineData(BucketType.Memcached, typeof(MemcachedBucket))]
        public void Create_GivenType_ExpectedType(BucketType bucketType, Type expectedType)
        {
            // Arrange

            var bucketFactory = new BucketFactory(
                new ClusterContext(),
                new Mock<IScopeFactory>().Object,
                new Mock<IRetryOrchestrator>().Object,
                new Mock<IVBucketKeyMapperFactory>().Object,
                new Mock<IKetamaKeyMapperFactory>().Object,
                new Mock<ILogger<CouchbaseBucket>>().Object,
                new Mock<ILogger<MemcachedBucket>>().Object,
                new TypedRedactor(RedactionLevel.None),
                new Mock<IBootstrapperFactory>().Object,
                NoopRequestTracer.Instance,
                new Mock<IOperationConfigurator>().Object,
                new BestEffortRetryStrategy(),
                new Mock<IHttpClusterMapFactory>().Object,
                new Mock<IConfigPushHandlerFactory>().Object);

                // Act

            var result = bucketFactory.Create("bucket_name", bucketType, new BucketConfig());

            // Assert

            Assert.IsAssignableFrom(expectedType, result);
            Assert.Equal("bucket_name", result.Name);
        }

        [Fact]
        public void Create_UnknownType_ArgumentOutOfRangeException()
        {
            // Arrange

            var bucketFactory = new BucketFactory(
                new ClusterContext(),
                new Mock<IScopeFactory>().Object,
                new Mock<IRetryOrchestrator>().Object,
                new Mock<IVBucketKeyMapperFactory>().Object,
                new Mock<IKetamaKeyMapperFactory>().Object,
                new Mock<ILogger<CouchbaseBucket>>().Object,
                new Mock<ILogger<MemcachedBucket>>().Object,
                new TypedRedactor(RedactionLevel.None),
                new Mock<IBootstrapperFactory>().Object,
                NoopRequestTracer.Instance,
                new Mock<IOperationConfigurator>().Object,
                new BestEffortRetryStrategy(),
                new Mock<IHttpClusterMapFactory>().Object,
                new Mock<IConfigPushHandlerFactory>().Object);

            // Act/Assert

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                bucketFactory.Create("bucket_name", (BucketType) 500, new BucketConfig()));

            Assert.Equal("bucketType", ex.ParamName);
        }

        #endregion
    }
}

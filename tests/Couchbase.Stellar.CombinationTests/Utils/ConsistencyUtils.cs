using System;
using System.Threading.Tasks;
using Couchbase.KeyValue;
using Couchbase.Stellar.CombinationTests.Fixtures;

namespace Couchbase.Stellar.CombinationTests.Utils;

public class ConsistencyUtils
{
    private StellarFixture? _fixture;

    public ConsistencyUtils(StellarFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task WaitUntilBucketIsPresent(string bucketName, int limit = 10)
    {
        bool isPresent = false;
        short retryCount = 0;
        while (!isPresent && retryCount < 10)
        {
            try
            {
                await _fixture!.CouchbaseCluster.Buckets.GetBucketAsync(bucketName).ConfigureAwait(false);
            }
            catch (Exception)
            {
                retryCount++;
                await Task.Delay(LinearBackoff(retryCount)).ConfigureAwait(false);
                continue;
            }
            isPresent = true;
        }

        if (!isPresent) throw new CouchbaseException($"The consistency util failed out after {limit} attempts.");
    }

    public async Task WaitUntilScopeIsPresent(string scopeName = "_default", string bucketName = "default", int limit = 10)
    {
        bool isPresent = false;
        short retryCount = 0;
        while (!isPresent && retryCount < 10)
        {
            try
            {
                var bucket = await _fixture!.CouchbaseCluster.BucketAsync(bucketName).ConfigureAwait(false);
                await bucket.ScopeAsync(scopeName).ConfigureAwait(false);
            }
            catch (Exception)
            {
                retryCount++;
                await Task.Delay(LinearBackoff(retryCount)).ConfigureAwait(false);
                continue;
            }
            isPresent = true;
        }

        if (!isPresent) throw new CouchbaseException($"The consistency util failed out after {limit} attempts.");
    }

    public async Task WaitUntilCollectionIsPresent(string collectionName, string? bucketName = null, string? scopeName = null, int limit = 10)
    {
        bool isPresent = false;
        short retryCount = 0;
        var docId = Guid.NewGuid().ToString();
        while (!isPresent && retryCount < limit)
        {
            try
            {
                var bucket = await _fixture!.CouchbaseCluster.BucketAsync(bucketName ?? "default").ConfigureAwait(false);
                var scope = await bucket.ScopeAsync(scopeName ?? "_default").ConfigureAwait(false);
                var collection = await scope.CollectionAsync(collectionName ?? "_default").ConfigureAwait(false);
                await collection.UpsertAsync(docId, new { Content = "Content" }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                retryCount++;
                await Task.Delay(LinearBackoff(retryCount)).ConfigureAwait(false);
                continue;
            }
            isPresent = true;
        }

        if (!isPresent) throw new CouchbaseException($"The consistency util failed out after {limit} attempts.");
    }

    public async Task WaitUntilCollectionIsDropped(string collectionName, string? bucketName = null, string? scopeName = null, int limit = 10)
    {
        var bucket = await _fixture!.CouchbaseCluster.BucketAsync(bucketName ?? "default").ConfigureAwait(false);
        var scope = await bucket.ScopeAsync(scopeName ?? "_default").ConfigureAwait(false);

        bool isGone = false;
        short retryCount = 0;
        while (!isGone && retryCount < limit)
        {
            try
            {
                var collection = await scope.CollectionAsync(collectionName).ConfigureAwait(false);
                await collection.UpsertAsync(Guid.NewGuid().ToString(), new { TestContent = "Test" }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                isGone = true;
                break;
            }
            retryCount++;
            await Task.Delay(LinearBackoff(retryCount)).ConfigureAwait(false);
        }

        if (!isGone) throw new CouchbaseException($"The consistency util failed out after {limit} attempts.");
    }

    public async Task WaitUntilDocumentIsPresent(string id, string? collectionName = null, string? bucketName = null, string? scopeName = null, int limit = 10)
    {
        IGetResult? result = null;

        bool isPresent = false;
        short retryCount = 0;
        while (!isPresent && retryCount < limit)
        {
            try
            {
                var bucket = await _fixture!.CouchbaseCluster.BucketAsync(bucketName ?? "default").ConfigureAwait(false);
                var scope = await bucket.ScopeAsync(scopeName ?? "_default").ConfigureAwait(false);
                var collection = await scope.CollectionAsync(collectionName ?? "_default").ConfigureAwait(false);
                result = await collection.GetAsync(id).ConfigureAwait(false);
            }
            catch (Exception)
            {
                retryCount++;
                await Task.Delay(LinearBackoff(retryCount)).ConfigureAwait(false);
            }

            if (result != null)
            {
                isPresent = true;
                break;
            }

            if (!isPresent) throw new CouchbaseException($"The consistency util failed out after {limit} attempts.");
        }
    }

    private static TimeSpan LinearBackoff(int retryCount)
    {
        return TimeSpan.FromMilliseconds(retryCount * 200);
    }
}

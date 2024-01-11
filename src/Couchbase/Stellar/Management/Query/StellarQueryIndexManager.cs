#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase.Protostellar.Admin.Query.V1;
using Couchbase.Management.Query;
using Couchbase.Stellar.Util;
using Google.Protobuf.Collections;
using IndexType = Couchbase.Management.Views.IndexType;

namespace Couchbase.Stellar.Management.Query;

#nullable enable

internal class StellarQueryIndexManager : IQueryIndexManager
{
    private readonly StellarCluster _stellarCluster;
    private readonly QueryAdminService.QueryAdminServiceClient _stellarQueryAdminClient;
    public StellarQueryIndexManager(StellarCluster stellarCluster)
    {
        _stellarQueryAdminClient = new QueryAdminService.QueryAdminServiceClient(stellarCluster.GrpcChannel);
        _stellarCluster = stellarCluster;
    }
    public async Task<IEnumerable<QueryIndex>> GetAllIndexesAsync(string bucketName, GetAllQueryIndexOptions? options = null)
    {
        var opts = options?.AsReadOnly() ?? GetAllQueryIndexOptions.DefaultReadOnly;

        var protoRequest = new GetAllIndexesRequest
        {
            BucketName = bucketName
        };
        if (opts.ScopeNameValue != null) protoRequest.ScopeName = opts.ScopeNameValue;
        if (opts.CollectionNameValue != null) protoRequest.CollectionName = opts.CollectionNameValue;

        var response = await _stellarQueryAdminClient.GetAllIndexesAsync(protoRequest, _stellarCluster.GrpcCallOptions()).ConfigureAwait(false);

        //TODO: Should be ok using the null-coalescing since we're parsing Proto -> Core
        var coreIndexes = response.Indexes.Select(i => new QueryIndex
        {
            Name = i.Name,
            IsPrimary = i.IsPrimary,
            Type = Enum.Parse<IndexType>(i.Type.ToString(), true),
            State = i.State.ToString(),
            Keyspace = null,
            Partition = i.Partition ?? null,
            Condition = i.Condition ?? null,
            IndexKey = null,
            ScopeName = i.ScopeName,
            BucketName = i.BucketName
        });
        return coreIndexes;
    }

    public async Task CreateIndexAsync(string bucketName, string indexName, IEnumerable<string> indexKeys,
        CreateQueryIndexOptions? options = null)
    {
        var opts = options?.AsReadOnly() ?? CreateQueryIndexOptions.DefaultReadOnly;
        var protoRequest = new CreateIndexRequest
        {
            BucketName = bucketName,
            Name = indexName,
            Deferred = opts.DeferredValue,
            IgnoreIfExists = opts.IgnoreIfExistsValue
        };
        if (opts.ScopeNameValue != null) protoRequest.ScopeName = opts.ScopeNameValue;
        if (opts.CollectionNameValue != null) protoRequest.CollectionName = opts.CollectionNameValue;
        protoRequest.Fields.AddRange(indexKeys);

        await _stellarQueryAdminClient.CreateIndexAsync(protoRequest, _stellarCluster.GrpcCallOptions()).ConfigureAwait(false);
    }

    public async Task CreatePrimaryIndexAsync(string bucketName, CreatePrimaryQueryIndexOptions? options = null)
    {
        var opts = options?.AsReadOnly() ?? CreatePrimaryQueryIndexOptions.DefaultReadOnly;
        var protoRequest = new CreatePrimaryIndexRequest
        {
            BucketName = bucketName,
            Deferred = opts.DeferredValue,
            IgnoreIfExists = opts.IgnoreIfExistsValue
        };
        if (opts.ScopeNameValue != null) protoRequest.ScopeName = opts.ScopeNameValue;
        if (opts.CollectionNameValue != null) protoRequest.CollectionName = opts.CollectionNameValue;
        if (opts.IndexNameValue != null) protoRequest.Name = opts.IndexNameValue;

        await _stellarQueryAdminClient.CreatePrimaryIndexAsync(protoRequest, _stellarCluster.GrpcCallOptions()).ConfigureAwait(false);
    }

    public async Task DropIndexAsync(string bucketName, string indexName, DropQueryIndexOptions? options = null)
    {
        var opts = options?.AsReadOnly() ?? DropQueryIndexOptions.DefaultReadOnly;
        var protoRequest = new DropIndexRequest
        {
            BucketName = bucketName,
            Name = indexName,
            IgnoreIfMissing = !opts.IgnoreIfExistsValue //TODO: is this right?
        };
        if (opts.ScopeNameValue != null) protoRequest.ScopeName = opts.ScopeNameValue;
        if (opts.CollectionNameValue != null) protoRequest.CollectionName = opts.CollectionNameValue;

        await _stellarQueryAdminClient.DropIndexAsync(protoRequest, _stellarCluster.GrpcCallOptions()).ConfigureAwait(false);
    }

    public async Task DropPrimaryIndexAsync(string bucketName, DropPrimaryQueryIndexOptions? options = null)
    {
        var opts = options?.AsReadOnly() ?? DropPrimaryQueryIndexOptions.DefaultReadOnly;
        var protoRequest = new DropPrimaryIndexRequest
        {
            BucketName = bucketName,
            IgnoreIfMissing = !opts.IgnoreIfExistsValue //TODO: Is this right?
        };
        if (opts.IndexNameValue != null) protoRequest.Name = opts.IndexNameValue;
        if (opts.ScopeNameValue != null) protoRequest.ScopeName = opts.ScopeNameValue;
        if (opts.CollectionNameValue != null) protoRequest.CollectionName = opts.CollectionNameValue;

        await _stellarQueryAdminClient.DropPrimaryIndexAsync(protoRequest, _stellarCluster.GrpcCallOptions()).ConfigureAwait(false);
    }

    public async Task BuildDeferredIndexesAsync(string bucketName, BuildDeferredQueryIndexOptions? options = null)
    {
        var opts = options?.AsReadOnly() ?? BuildDeferredQueryIndexOptions.DefaultReadOnly;
        var protoRequest = new BuildDeferredIndexesRequest
        {
            BucketName = bucketName,
        };
        if (opts.ScopeNameValue != null) protoRequest.ScopeName = opts.ScopeNameValue;
        if (opts.CollectionNameValue != null) protoRequest.CollectionName = opts.CollectionNameValue;

        await _stellarQueryAdminClient.BuildDeferredIndexesAsync(protoRequest, _stellarCluster.GrpcCallOptions()).ConfigureAwait(false);
    }

    public Task WatchIndexesAsync(string bucketName, IEnumerable<string> indexNames, WatchQueryIndexOptions? options = null)
    {
        throw new UnsupportedInProtostellarException(nameof(WatchIndexesAsync));
    }
}
#endif

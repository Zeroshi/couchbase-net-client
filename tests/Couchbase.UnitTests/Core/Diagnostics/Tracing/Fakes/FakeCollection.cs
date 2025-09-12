using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Couchbase.KeyValue;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Utils;
using Couchbase.KeyValue.RangeScan;
using Couchbase.Management.Query;
using Moq;

#nullable enable

namespace Couchbase.UnitTests.Core.Diagnostics.Tracing.Fakes
{
    internal class FakeCollection : ICouchbaseCollection
    {
        private readonly IBucket _bucket;
        private readonly ClusterOptions _clusterOptions;

        public FakeCollection(string name, IScope scope, IBucket bucket, ClusterOptions clusterOptions)
        {
            _bucket = bucket;
            _clusterOptions = clusterOptions;
            Name = name;
            Scope = scope;
        }

        public uint? Cid { get; }
        public string Name { get; }
        public IScope Scope { get; }
        public IBinaryCollection Binary { get; } = null!;
        public bool IsDefaultCollection => Scope.IsDefaultScope && Name == "_default";
        public bool AccessDeleted => false;

        public Task<IGetResult> GetAsync(string id, GetOptions? options = null)
        {
            using var rootSpan = RootSpan(OuterRequestSpans.ServiceSpan.Kv.Get);
            var op = new FakeOperation
            {
                Span2 = rootSpan
            };

           ((FakeBucket) _bucket).SendAsync(op);

           return Task.FromResult(new Mock<IGetResult>().Object);
        }

        public Task<IExistsResult> ExistsAsync(string id, ExistsOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IMutationResult> UpsertAsync<T>(string id, T content, UpsertOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IMutationResult> InsertAsync<T>(string id, T content, InsertOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IMutationResult> ReplaceAsync<T>(string id, T content, ReplaceOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string id, RemoveOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task UnlockAsync<T>(string id, ulong cas, UnlockOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task UnlockAsync(string id, ulong cas, UnlockOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task TouchAsync(string id, TimeSpan expiry, TouchOptions? options)
        {
            throw new NotImplementedException();
        }

        public Task<IMutationResult> TouchWithCasAsync(string id, TimeSpan expiry, TouchOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IGetResult> GetAndTouchAsync(string id, TimeSpan expiry, GetAndTouchOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IGetResult> GetAndLockAsync(string id, TimeSpan expiry, GetAndLockOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IGetReplicaResult> GetAnyReplicaAsync(string id, GetAnyReplicaOptions? options = null)
        {
            using var rootSpan = RootSpan(OuterRequestSpans.ServiceSpan.Kv.Get);
            var op = new FakeOperation
            {
                Span2 = rootSpan
            };

            ((FakeBucket)_bucket).SendAsync(op);

            var childOp = new FakeOperation
            {
                Span2 = rootSpan
            };

            ((FakeBucket)_bucket).SendAsync(childOp);

            var childOp2 = new FakeOperation
            {
                Span2 = rootSpan
            };

            ((FakeBucket)_bucket).SendAsync(childOp2);

            return Task.FromResult(new Mock<IGetReplicaResult>().Object);
        }

        public IEnumerable<Task<IGetReplicaResult>> GetAllReplicasAsync(string id, GetAllReplicasOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<ILookupInResult> LookupInAsync(string id, IEnumerable<LookupInSpec> specs, LookupInOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<ILookupInReplicaResult> LookupInAnyReplicaAsync(string id, IEnumerable<LookupInSpec> specs, LookupInAnyReplicaOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<ILookupInReplicaResult> LookupInAllReplicasAsync(string id, IEnumerable<LookupInSpec> specs, LookupInAllReplicasOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public Task<IMutateInResult> MutateInAsync(string id, IEnumerable<MutateInSpec> specs, MutateInOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public string ScopeName => Scope.Name;

        private IRequestSpan RootSpan(string operation)
        {
            var span = _clusterOptions.TracingOptions.RequestTracer?.RequestSpan(operation);
            span!.SetAttribute(OuterRequestSpans.Attributes.System.Key, OuterRequestSpans.Attributes.System.Value);
            span.SetAttribute(OuterRequestSpans.Attributes.Service, nameof(OuterRequestSpans.ServiceSpan.Kv).ToLowerInvariant());
            span.SetAttribute(OuterRequestSpans.Attributes.BucketName, _bucket.Name);
            span.SetAttribute(OuterRequestSpans.Attributes.ScopeName, ScopeName);
            span.SetAttribute(OuterRequestSpans.Attributes.CollectionName, Name);
            span.SetAttribute(OuterRequestSpans.Attributes.Operation, operation);
            return span;
        }

        public IAsyncEnumerable<IScanResult> ScanAsync(IScanType scanType, ScanOptions? options = null)
        {
            throw new NotImplementedException();
        }

        public ICollectionQueryIndexManager QueryIndexes { get; } = null!;
    }
}

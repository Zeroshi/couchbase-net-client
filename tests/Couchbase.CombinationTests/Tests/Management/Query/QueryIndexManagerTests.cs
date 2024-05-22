using System;
using Couchbase.CombinationTests.Fixtures;
using Couchbase.Core.Exceptions;
using Couchbase.Management.Query;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.CombinationTests.Tests.Management.Query;

[Collection(CombinationTestingCollection.Name)]
public class QueryIndexManagerTests
{
    private readonly CouchbaseFixture _fixture;
    private readonly ITestOutputHelper _outputHelper;
    private TestHelper _testHelper;
    private volatile bool _isBucketFlushed;
    private IQueryIndexManager _queryIndexes;

    public QueryIndexManagerTests(CouchbaseFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        _outputHelper = outputHelper;
        _testHelper = new TestHelper(fixture);
        _queryIndexes = _fixture.Cluster.QueryIndexes;
    }

    #region Assert Timeout is Propagated
    [Fact]
    public async void GetAllIndexesAsync_With_Zero_Timeout_Throws()
    {
        var exception = await Record.ExceptionAsync( () =>  _queryIndexes.GetAllIndexesAsync("default", opts => opts.Timeout(TimeSpan.FromMilliseconds(1))));
        Assert.IsType<AmbiguousTimeoutException>(exception);
    }

    [Fact]
    public async void CreateIndexAsync_With_Zero_Timeout_Throws()
    {
        var exception = await Record.ExceptionAsync( () =>  _queryIndexes.CreateIndexAsync("default", "index", new[] {"test"}, options => options.Timeout(TimeSpan.FromMilliseconds(1))));
        Assert.IsType<AmbiguousTimeoutException>(exception);
    }

    [Fact]
    public async void CreatePrimaryIndexAsync_With_Zero_Timeout_Throws()
    {
        var exception = await Record.ExceptionAsync( () =>  _queryIndexes.CreatePrimaryIndexAsync("default", opts => opts.Timeout(TimeSpan.FromMilliseconds(1))));
        Assert.IsType<AmbiguousTimeoutException>(exception);
    }

    [Fact]
    public async void DropIndexAsync_With_Zero_Timeout_Throws()
    {
        var exception = await Record.ExceptionAsync( () =>  _queryIndexes.DropIndexAsync("default", "index", opts => opts.Timeout(TimeSpan.FromMilliseconds(1))));
        Assert.IsType<AmbiguousTimeoutException>(exception);
    }

    [Fact]
    public async void DropPrimaryIndexAsync_With_Zero_Timeout_Throws()
    {
        var exception = await Record.ExceptionAsync( () =>  _queryIndexes.DropPrimaryIndexAsync("default", opts => opts.Timeout(TimeSpan.FromMilliseconds(1))));
        Assert.IsType<AmbiguousTimeoutException>(exception);
    }

    [Fact]
    public async void BuildDeferredIndexesAsync_With_Zero_Timeout_Throws()
    {
        var exception = await Record.ExceptionAsync( () =>  _queryIndexes.BuildDeferredIndexesAsync("default", opts => opts.Timeout(TimeSpan.FromMilliseconds(1))));
        Assert.IsType<AmbiguousTimeoutException>(exception);
    }

    //WatchIndexesAsync catches inner exceptions and doesn't throw
    #endregion
}

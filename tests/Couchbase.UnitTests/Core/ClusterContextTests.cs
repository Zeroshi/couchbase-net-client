using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.Core.CircuitBreakers;
using Couchbase.Core.Configuration.Server;
using Couchbase.Core.DI;
using Couchbase.Core.Diagnostics.Metrics.AppTelemetry;
using Couchbase.Core.Diagnostics.Tracing;
using Couchbase.Core.Diagnostics.Tracing.ThresholdTracing;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Core.IO.Connections;
using Couchbase.Core.IO.Operations;
using Couchbase.Core.Logging;
using Couchbase.UnitTests.Core.Diagnostics.Tracing.Fakes;
using Couchbase.UnitTests.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Moq;
using Xunit;
using Xunit.Abstractions;
using TraceListener = Couchbase.Core.Diagnostics.Tracing.TraceListener;

#pragma warning disable CS8632
namespace Couchbase.UnitTests.Core
{
    public class ClusterContextTests
    {
        private readonly ITestOutputHelper _output;

        public ClusterContextTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(@"Documents\Configs\config-localhost-alt-addresses-8093.json", 8093)]
        [InlineData(@"Documents\Configs\config-localhost-alt-addresses-5555.json", 5555)]
        public void Use_Alternate_Address_Query_Port(string configPath, int expectedPort)
        {
            // Arrange

            var config = ResourceHelper.ReadResource(configPath, InternalSerializationContext.Default.BucketConfig);
            var options = new ClusterOptions
            {
                NetworkResolution = NetworkResolution.External
            };
            config.SetEffectiveNetworkResolution(options);

            var nodeAdapter = new NodeAdapter(null, config.NodesExt.First(), config);
            var clusterNode = CreateMockedNode("localhost", 11210, nodeAdapter);

            var context = new ClusterContext();
            context.AddNode(clusterNode);

            // Act

            var serviceUriProvider = new ServiceUriProvider(context);
            var uri = serviceUriProvider.GetRandomQueryUri();

            //Assert

            Assert.Equal(expectedPort, uri.Port);
        }

        [Fact]
        public void PruneNodes_Removes_Rebalanced_Node()
        {
            //Arrange

            var config = ResourceHelper.ReadResource(@"Documents\Configs\config-error.json",
                InternalSerializationContext.Default.BucketConfig);
            var context = new ClusterContext();

            var hosts = new List<string>{"10.143.194.101", "10.143.194.102", "10.143.194.103", "10.143.194.104"};
            hosts.ForEach(x => context.AddNode(CreateMockedNode(x, 11210)));

            //Act

            context.PruneNodes(config);

            //Assert

            var removed = new HostEndpointWithPort("10.143.194.102", 11210);

            Assert.DoesNotContain(context.Nodes, node => node.EndPoint.Equals(removed));
        }

        [Fact]
        public async Task Bootstrap_Uses_Random_Seed_Nodes()
        {
            // set up a mock that records which node was chosen, then immediately faults
            ConcurrentDictionary<string, int> chosenNodes = new();
            var nodeFactoryMock = new Mock<IClusterNodeFactory>(MockBehavior.Strict);
            nodeFactoryMock.Setup(f =>
                    f.CreateAndConnectAsync(It.IsAny<HostEndpointWithPort>(), It.IsAny<CancellationToken>()))
                .Returns((HostEndpointWithPort host, CancellationToken token) =>
                {
                    chosenNodes.AddOrUpdate(host.Host,
                        addValueFactory: s => 0,
                        updateValueFactory: (s, count) => count + 1);
                    throw new CouchbaseException(new KeyValueErrorContext()
                    {
                        Status = ResponseStatus.BucketNotConnected
                    }, "break early");
                    // return Task.FromResult(CreateMockedNode(host.Host, host.Port));
                });
            var options = new ClusterOptions().WithConnectionString("couchbase://node1,node2,node3?random_seed_nodes=true");
            options.EnableDnsSrvResolution = false;
            options.AddClusterService<IClusterNodeFactory>(nodeFactoryMock.Object);
            using var cts = new CancellationTokenSource();
            var context = new ClusterContext(cts, options);

            // call bootstrap enough times for random behavior to become apparent.
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    await context.BootstrapGlobalAsync();
                }
                catch
                { }
            }

            foreach (var kvp in chosenNodes)
            {
                _output.WriteLine($"{kvp.Key} = {kvp.Value}");
            }

            // if nodes are being chosen randomly, there should be more than one entry in the "chosenNodes" list.
            // before fixing the issue, only "node1" was chosen.
            Assert.NotEmpty(chosenNodes);
            Assert.InRange(chosenNodes.Count, 2, 3);
        }

        private IClusterNode CreateMockedNode(string hostname, int port, NodeAdapter nodeAdapter = null)
        {
            var mockConnectionPool = new Mock<IConnectionPool>();

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(m => m.Create(It.IsAny<ClusterNode>()))
                .Returns(mockConnectionPool.Object);

            nodeAdapter ??= new NodeAdapter
            {
                Hostname = hostname,
                KeyValue = port
            };

            var clusterNode = new ClusterNode(new ClusterContext(), mockConnectionPoolFactory.Object,
                new Mock<ILogger<ClusterNode>>().Object,
                new DefaultObjectPool<OperationBuilder>(new OperationBuilderPoolPolicy()),
                new Mock<ICircuitBreaker>().Object,
                new Mock<ISaslMechanismFactory>().Object,
                new TypedRedactor(RedactionLevel.None),
                new HostEndpointWithPort(hostname, port),
                nodeAdapter,
                NoopRequestTracer.Instance,
                new Mock<IOperationConfigurator>().Object
            )
            {
                Owner = new FakeBucket("default", new ClusterOptions())
            };

            return clusterNode;
        }

        #region Tracing

        [Fact]
        public void When_Tracing_Disabled_Custom_To_CustomTraceListener()
        {
            using var listener = new CustomTraceListener();

            var options = new ClusterOptions { TracingOptions = { Enabled = false } };
            options.WithThresholdTracing(new ThresholdOptions
            {
                Enabled = false,
                ThresholdListener = listener
            }).WithOrphanTracing(options => options.Enabled = false);

            var services = options.BuildServiceProvider();
            var noopRequestTracer = services.GetService(typeof(IRequestTracer));

            Assert.IsAssignableFrom<NoopRequestTracer>(noopRequestTracer);
        }

        [Fact]
        public async Task BootstrapGlobal_Should_Not_Swallow_AuthenticationFailure()
        {
            var options = new ClusterOptions().WithConnectionString("couchbases://localhost1,localhost2");
            var mockNodeFactory = new Mock<IClusterNodeFactory>(MockBehavior.Strict);
            mockNodeFactory.Setup(cnf => cnf.CreateAndConnectAsync(It.IsAny<HostEndpointWithPort>(), It.IsAny<CancellationToken>()))
                .Throws(new AuthenticationFailureException());
            options.AddClusterService(mockNodeFactory.Object);
            using var context = new ClusterContext(Mock.Of<ICluster>(), new CancellationTokenSource(), options);
            var ex = await Assert.ThrowsAsync<AuthenticationFailureException>(() => context.BootstrapGlobalAsync());
        }


        [Fact]
        public async Task BootstrapGlobal_Should_Continue_After_AuthenticationFailureException()
        {
            var options = new ClusterOptions().WithConnectionString("couchbase://localhost1,localhost2?random_seed_nodes=false");

            var mockNodeFactory = new Mock<IClusterNodeFactory>(MockBehavior.Loose);

            mockNodeFactory.Setup(cnf => cnf.CreateAndConnectAsync(new HostEndpointWithPort("localhost1", 11210), It.IsAny<CancellationToken>()))
                .Throws(new AuthenticationFailureException());

            var config = ResourceHelper.ReadResource(@"Documents\Configs\cluster-level-config-rev69.json",
                InternalSerializationContext.Default.BucketConfig);

            config.VBucketServerMap = new Couchbase.Core.Sharding.VBucketServerMapDto();

            var mockClusterNode = new Mock<IClusterNode>();
            mockClusterNode.Setup(cn => cn.GetClusterMap(null, default)).Returns(Task.FromResult(config));

            mockNodeFactory.Setup(cnf => cnf.CreateAndConnectAsync(new HostEndpointWithPort("localhost2", 11210), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(mockClusterNode.Object));

            options.AddClusterService(mockNodeFactory.Object);
            using var context = new ClusterContext(Mock.Of<ICluster>(), new CancellationTokenSource(), options);
            await context.BootstrapGlobalAsync();
        }

        [Fact]
        public void When_Tracing_Enabled_Custom_To_CustomTraceListener()
        {
            using var listener = new CustomTraceListener();

            var options = new ClusterOptions();
            options.WithThresholdTracing(new ThresholdOptions
            {
                Enabled = true,
                ThresholdListener = listener
            });

            using var context = new ClusterContext(Mock.Of<ICluster>(), new CancellationTokenSource(), options);
            context.Start();

            var tracer = context.ServiceProvider.GetRequiredService<IRequestTracer>();
            var span = tracer.RequestSpan("works");
            span.Dispose();

            var activities = listener.GetActivities().Where(x => x.OperationName == "works").ToArray();

            foreach (var activity in activities)
            {
                _output.WriteLine($"The name of the activity is '{activity.DisplayName}'");
            }
            Assert.Single(activities);
        }

        [Fact]
        public void When_Tracing_Enabled_Custom_To_CustomTraceListener_Not_Disposed()
        {
            using var listener = new CustomTraceListener();

            var options = new ClusterOptions();
            options.WithThresholdTracing(new ThresholdOptions
            {
                Enabled = true,
                ThresholdListener = listener
            });

            using (var context = new ClusterContext(Mock.Of<ICluster>(), new CancellationTokenSource(), options))
            {
                context.Start();

                var tracer = context.ServiceProvider.GetRequiredService<IRequestTracer>();
                var span = tracer.RequestSpan("works");
                span.Dispose();
            }

            Assert.False(listener.Disposed);
        }

        public class CustomTraceListener : TraceListener
        {
            public bool Disposed { get; private set; }

            public CustomTraceListener()
            {
                Start();
            }

            // Due to thread sync issues, a listener may receive the same activity more than once.
            // We use a hash set to avoid tracking it multiple times and breaking tests.
            private HashSet<Activity> _activities = new();

            public sealed override void Start()
            {
                Listener.ActivityStopped = activity =>
                {
                    // We may be receiving activities from other tests, so lock
                    lock (_activities)
                    {
                        _activities.Add(activity);
                    }
                };
                Listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                    ActivitySamplingResult.AllData;
                Listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                    ActivitySamplingResult.AllData;
                Listener.ShouldListenTo = s => true;
            }

            public Activity[] GetActivities()
            {
                lock (_activities)
                {
                    return _activities.ToArray();
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                Disposed = true;
            }
        }

        public class CustomRequestTracer : IRequestTracer
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IRequestSpan RequestSpan(string name, IRequestSpan parentSpan = null)
            {
                return new CustomRequestSpan();
            }

            public IRequestTracer Start(TraceListener listener)
            {
                return new CustomRequestTracer();
            }
        }

        public class CustomRequestSpan : IRequestSpan
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IRequestSpan SetAttribute(string key, bool value)
            {
                throw new NotImplementedException();
            }

            public IRequestSpan SetAttribute(string key, string value)
            {
                throw new NotImplementedException();
            }

            public IRequestSpan SetAttribute(string key, uint value)
            {
                throw new NotImplementedException();
            }

            public IRequestSpan AddEvent(string name, DateTimeOffset? timestamp = null)
            {
                throw new NotImplementedException();
            }

            public void End()
            {
                throw new NotImplementedException();
            }

            public IRequestSpan? Parent { get; set; }
            public IRequestSpan ChildSpan(string name)
            {
                throw new NotImplementedException();
            }

            public bool CanWrite { get; }
            public string? Id { get; }
            public uint? Duration { get; }
        }

        #endregion
    }
}

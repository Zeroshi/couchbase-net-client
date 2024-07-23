using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase.Core.Diagnostics.Metrics;
using Couchbase.Core.IO.Operations;
using Couchbase.Extensions.Metrics.Otel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace Couchbase.Extensions.OpenTelemetry.UnitTests
{
    public class MeterTests
    {
        [Fact(Skip = "NCBC-3254")]
        public async Task BasicMetric_IsExported()
        {
            // Arrange

            var exportedItems = new List<Metric>();

            using var tracerProvider = Sdk.CreateMeterProviderBuilder()
                .AddCouchbaseInstrumentation()
                .AddInMemoryExporter(exportedItems, options =>
                {
                    options.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1;
                    options.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;
                })
                .Build();

            var operation = new Get<object>
            {
                BucketName = "bucket",
                CName = "_default",
                SName = "_default"
            };

            // Act

            MetricTracker.KeyValue.TrackOperation(operation, TimeSpan.FromSeconds(1), null);

            // Give the exporter time
            await Task.Delay(100);
            tracerProvider.ForceFlush();

            // Shut down tracer provider to prevent simultaneous access to the List<T>
            tracerProvider.Shutdown();
            await Task.Delay(100);

            // Assert

            IEnumerable<MetricPoint> Enumerate(Metric metric)
            {
                var enumerator = metric.GetMetricPoints().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }

            var duration = exportedItems
                .Where(p => p.Name == "db.couchbase.operations")
                .SelectMany(Enumerate)
                .Last();

            Assert.Equal(1000000, duration.GetHistogramSum());

            var count = exportedItems
                .Where(p => p.Name == "db.couchbase.operations.count")
                .SelectMany(Enumerate)
                .Last();

            Assert.Equal(1, count.GetSumLong());
        }
    }
}

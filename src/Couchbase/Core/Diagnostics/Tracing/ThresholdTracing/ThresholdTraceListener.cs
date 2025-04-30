using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Couchbase.Core.Logging;
using Couchbase.Utils;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Couchbase.Core.Diagnostics.Tracing.ThresholdTracing
{
    /// <summary>
    /// A <see cref="TraceListener"/> for <see cref="RequestTracer"/>; listens for any span closings and
    /// generates a <see cref="ThresholdSummary"/> if a span exceeds the threshold configured in <see cref="ThresholdOptions"/>.
    /// </summary>
    internal sealed partial class ThresholdTraceListener : TraceListener
    {
        private readonly IReadOnlyDictionary<string, TimeSpan> _serviceThresholds;
        private readonly Timer _timer;

        public ThresholdTraceListener(ILoggerFactory loggerFactory, ThresholdOptions options)
        {
            var thresholdOptions1 = options;
            var logger = loggerFactory.CreateLogger<RequestTracer>();
            _timer = TimerFactory.CreateWithFlowSuppressed(GenerateAndLogReport, logger, thresholdOptions1.EmitInterval, thresholdOptions1.EmitInterval);
            ThresholdServiceQueue.SetSampleSize((int)thresholdOptions1.SampleSize);//change to uint

            _serviceThresholds = options.GetServiceThresholds();
            Start();
        }

        private static void GenerateAndLogReport(object? state)
        {
            ILogger? logger = null;
            try
            {
                logger = state as ILogger;
                var reportSummaries = ThresholdServiceQueue.ReportSummaries();

                if (reportSummaries.Count > 0 && logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    LogThresholdEvent(logger,
                        JsonSerializer.Serialize(reportSummaries, ThresholdTracingSerializerContext.Default.IDictionaryStringThresholdSummaryReport));
                }
            }
            catch (Exception e)
            {
                if (logger is not null)
                {
                    LogReportError(logger, e);
                }
            }
        }

        /// <inheritdoc />
        public sealed override void Start()
        {
            Listener.ActivityStopped = activity =>
            {
                var serviceAttribute = activity.Tags.FirstOrDefault(tag => tag.Key == OuterRequestSpans.Attributes.Service);
                if (serviceAttribute.Value == null) return;

                if (_serviceThresholds.TryGetValue(serviceAttribute.Value, out var threshold))
                {
                    if (activity.Duration > threshold)
                    {
                        var summary = ThresholdSummary.FromActivity(activity);
                        ThresholdServiceQueue.AddByService(serviceAttribute.Value, summary);
                    }
                }
            };
            Listener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) =>
                ActivitySamplingResult.AllData;
            Listener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) =>
                ActivitySamplingResult.AllData;
            Listener.ShouldListenTo = s => s.Name == RequestTracer.ActivitySourceName;
        }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            finally
            {
                _timer?.Dispose();
            }
        }

        [LoggerMessage(LoggingEvents.ThresholdEvent, LogLevel.Information, "{message}")]
        private static partial void LogThresholdEvent(ILogger logger, string message);

        [LoggerMessage(200, LogLevel.Error, "ThresholdRequestLogging report generation failed.")]
        private static partial void LogReportError(ILogger logger, Exception ex);
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

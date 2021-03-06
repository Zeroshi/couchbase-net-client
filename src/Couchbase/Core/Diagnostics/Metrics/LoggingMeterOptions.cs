using System;
using Microsoft.Extensions.Logging;

namespace Couchbase.Core.Diagnostics.Metrics
{
    /// <summary>
    /// Options for <see cref="LoggingMeter"/> instances.
    /// </summary>
    public class LoggingMeterOptions
    {
        internal TimeSpan EmitIntervalValue { get; set; } = TimeSpan.FromSeconds(600);

        /// <summary>
        /// The interval after which the aggregated trace information is logged.
        /// </summary>
        /// <remarks>Defaults to 600 seconds.</remarks>
        /// <param name="emitInterval"></param>
        /// <returns>A <see cref="LoggingMeterOptions"/> instance for chaining.</returns>
        public LoggingMeterOptions EmitInterval(TimeSpan emitInterval)
        {
            EmitIntervalValue = emitInterval;
            return this;
        }

        internal bool EnabledValue { get; set; } = true;

        /// <summary>
        /// Stops the meter from collecting data.
        /// </summary>
        /// <param name="enabled">A <see cref="bool"/> for stopping or starting collecting.</param>
        /// <returns>A <see cref="LoggingMeterOptions"/> instance for chaining.</returns>
        public LoggingMeterOptions Enabled(bool enabled)
        {
            EnabledValue = enabled;
            return this;
        }

        internal bool ReportingEnabledValue { get; set; } = true;

        /// <summary>
        /// Stops the meter from reporting on collected data.
        /// </summary>
        /// <param name="reportingEnabled">A <see cref="bool"/> for stopping or starting reporting.</param>
        /// <returns>A <see cref="LoggingMeterOptions"/> instance for chaining.</returns>
        public LoggingMeterOptions ReportingEnabled(bool reportingEnabled)
        {
            ReportingEnabledValue = reportingEnabled;
            return this;
        }

        internal IMeter LoggingMeterValue { get; set; }
        public LoggingMeterOptions LoggingMeter(IMeter meter)
        {
            LoggingMeterValue = meter;
            return this;
        }

        internal IMeter CreateMeter(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new NullReferenceException(nameof(loggerFactory));
            }
            return LoggingMeterValue ??= new LoggingMeter(loggerFactory, this);
        }
    }
}

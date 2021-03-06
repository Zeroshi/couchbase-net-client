#nullable enable
using System;
using System.Diagnostics;
using Couchbase.Core.Diagnostics.Tracing;

namespace Couchbase.Extensions.Tracing.Otel.Tracing
{
    internal class OpenTelemetryRequestSpan : IRequestSpan
    {
        private readonly IRequestTracer _tracer;
        private readonly Activity? _activity;
        private readonly IRequestSpan? _parentSpan;

        public OpenTelemetryRequestSpan(IRequestTracer tracer, Activity? activity, IRequestSpan? parentSpan = null)
        {
            _tracer = tracer;
            _activity = activity;
            _parentSpan = parentSpan;
            _activity?.SetStartTime(DateTime.UtcNow);
        }

        /// <inheritdoc />
        public bool CanWrite { get; } = true;

        /// <inheritdoc />
        public string? Id => _activity?.Id;

        public uint? Duration { get; private set; }

        /// <inheritdoc />
        public IRequestSpan? Parent { get; set; }

        /// <inheritdoc />
        public IRequestSpan SetAttribute(string key, string value)
        {
            _activity?.AddTag(key, value);
            return this;
        }

        /// <inheritdoc />
        public IRequestSpan SetAttribute(string key, uint value)
        {
            _activity?.AddTag(key, value);
            return this;
        }

        /// <inheritdoc />
        public IRequestSpan SetAttribute(string key, bool value)
        {
            _activity?.AddTag(key, value);
            return this;
        }

        /// <inheritdoc />
        public IRequestSpan AddEvent(string name, DateTimeOffset? timestamp = null)
        {
            var activityEvent = new ActivityEvent(name, timestamp ?? default);
            _activity?.AddEvent(activityEvent);
            return this;
        }

        public IRequestSpan ChildSpan(string name)
        {
            return _tracer.RequestSpan(name, this);
        }

        public void End()
        {
            //temp implementation
            _activity?.SetEndTime(DateTime.UtcNow);
            _activity?.Stop();
        }

        public void Dispose()
        {
            End();
        }

    }
}

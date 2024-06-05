#if NET5_0_OR_GREATER
#nullable enable

// ReSharper disable InconsistentNaming

namespace Couchbase.Integrated.Transactions.DataModel
{
    internal class QueryErrorCause
    {
        public QueryErrorCause(object? cause, bool? rollback, bool? retry, string? raise)
        {
            this.cause = cause;
            this.rollback = rollback;
            this.retry = retry;
            this.raise = raise;
        }

        public object? cause { get;  }
        public bool? rollback { get; }
        public bool? retry { get; }
        public string? raise { get; }
    }
}
#endif

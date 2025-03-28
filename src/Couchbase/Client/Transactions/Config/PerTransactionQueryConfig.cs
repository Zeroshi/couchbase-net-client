using Couchbase.Query;

namespace Couchbase.Client.Transactions.Config
{
    /// <summary>
    /// Allows setting a per-transaction query configuration.
    /// </summary>
    public class PerTransactionQueryConfig
    {
        /// <summary>
        /// Gets or sets the index scan consistency for query operations.
        /// </summary>
        public QueryScanConsistency? ScanConsistency { get; set; }
    }
}

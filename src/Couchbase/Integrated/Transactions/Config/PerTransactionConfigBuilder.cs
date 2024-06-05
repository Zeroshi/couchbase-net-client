#if NET5_0_OR_GREATER
#nullable enable
using System;
using Couchbase.KeyValue;

namespace Couchbase.Integrated.Transactions.Config
{
    /// <summary>
    /// A builder class for generating <see cref="PerTransactionConfig"/>s to be used for individual transactions.
    /// </summary>
    internal class PerTransactionConfigBuilder
    {
        private readonly PerTransactionConfig _config;

        private PerTransactionConfigBuilder()
        {
            _config = new PerTransactionConfig();
        }

        /// <summary>
        /// Create an instance of the <see cref="PerTransactionConfigBuilder"/> class.
        /// </summary>
        /// <returns></returns>
        public static PerTransactionConfigBuilder Create() => new PerTransactionConfigBuilder();

        /// <summary>
        /// Set the minimum desired <see cref="DurabilityLevel(KeyValue.DurabilityLevel)"/>.
        /// </summary>
        /// <param name="durabilityLevel">The <see cref="DurabilityLevel(KeyValue.DurabilityLevel)"/> desired.</param>
        /// <returns>The continued instance of this builder.</returns>
        public PerTransactionConfigBuilder DurabilityLevel(DurabilityLevel durabilityLevel)
        {
            _config.DurabilityLevel = durabilityLevel;
            return this;
        }

        /// <summary>
        /// Sets the per-transaction specific query options.
        /// </summary>
        /// <param name="perTransactionQueryConfig">The per-transaction specific query options.</param>
        /// <returns>The continued instance of this builder.</returns>
        public PerTransactionConfigBuilder QueryConfig(PerTransactionQueryConfig perTransactionQueryConfig)
        {
            _config.ScanConsistency = perTransactionQueryConfig.ScanConsistency;
            return this;
        }

        /// <summary>
        /// Sets an optional value indicating the relative expiration time of the transaction for this transaction.
        /// </summary>
        /// <param name="timeout">The relative expiration time of the transaction for this transaction</param>
        /// <returns>The continued instance of this builder.</returns>
        public PerTransactionConfigBuilder Timeout(TimeSpan? timeout)
        {
            _config.Timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets an option value indicating the timeout on Couchbase Key/Value operations for this transaction.
        /// </summary>
        /// <param name="keyValueTimeout">The timeout on Couchbase Key/Value operations for this transaction</param>
        /// <returns>The continued instance of this builder.</returns>
        public PerTransactionConfigBuilder KeyValueTimeout(TimeSpan? keyValueTimeout)
        {
            _config.KeyValueTimeout = keyValueTimeout;
            return this;
        }

        /// <summary>
        /// Build a <see cref="PerTransactionConfig"/> from this builder.
        /// </summary>
        /// <returns>A completed config.</returns>
        public PerTransactionConfig Build() => _config;
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
#endif

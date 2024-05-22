using System;
using System.Threading;
using Couchbase.Utils;
using CancellationTokenCls = System.Threading.CancellationToken;

#nullable enable

namespace Couchbase.Management.Query
{
    public class WatchQueryIndexOptions
    {
        public static readonly ReadOnly DefaultReadOnly = Default.AsReadOnly();
        internal bool WatchPrimaryValue { get; set; }
        internal CancellationToken TokenValue { get; private set; } = CancellationTokenCls.None;
        internal TimeSpan TimeoutValue { get; set; } = ClusterOptions.Default.ManagementTimeout;
        internal string? ScopeNameValue { get; set; }
        internal string? CollectionNameValue { get; set; }

        internal string? QueryContext { get; set; }

        /// <summary>
        /// Sets the scope name for this query management operation.
        /// </summary>
        /// <remarks>If the scope name is set then the collection name must be set as well.</remarks>
        /// <param name="scopeName">The scope name to use.</param>
        /// <returns>A WatchQueryIndexOptions for chaining options.</returns>
        [Obsolete("Use collection.QueryIndexes instead.")]
        public WatchQueryIndexOptions ScopeName(string scopeName)
        {
            ScopeNameValue = scopeName;
            return this;
        }

        /// <summary>
        /// Sets the collection name for this query management operation.
        /// </summary>
        /// <remarks>If the collection name is set then the scope name must be set as well.</remarks>
        /// <param name="collectionName">The collection name to use.</param>
        /// <returns>A WatchQueryIndexOptions for chaining options.</returns>
        [Obsolete("Use collection.QueryIndexes instead.")]
        public WatchQueryIndexOptions CollectionName(string collectionName)
        {
            CollectionNameValue = collectionName;
            return this;
        }

        public WatchQueryIndexOptions WatchPrimary(bool watchPrimary)
        {
            WatchPrimaryValue = watchPrimary;
            return this;
        }

        /// <summary>
        /// Allows to pass in a custom CancellationToken from a CancellationTokenSource.
        /// Note that CancellationToken() takes precedence over Timeout(). If both CancellationToken and Timeout are set, the former will be used in the operation.
        /// </summary>
        /// <param name="cancellationToken">The Token to cancel the operation.</param>
        /// <returns>This class for method chaining.</returns>
        public WatchQueryIndexOptions CancellationToken(CancellationToken cancellationToken)
        {
            TokenValue = cancellationToken;
            return this;
        }

        /// <summary>
        /// Allows to set a Timeout for the operation.
        /// Note that CancellationToken() takes precedence over Timeout(). If both CancellationToken and Timeout are set, the former will be used in the operation.
        /// </summary>
        /// <param name="timeout">The duration of the Timeout. Set to 75s by default.</param>
        /// <returns>This class for method chaining.</returns>
        public WatchQueryIndexOptions Timeout(TimeSpan timeout)
        {
            TimeoutValue = timeout;
            return this;
        }

        public static WatchQueryIndexOptions Default => new WatchQueryIndexOptions();

        public void Deconstruct(out bool watchPrimaryValue, out CancellationToken tokenValue, out string? scopeNameValue, out string? collectionNameValue, out string? queryContext, out TimeSpan timeoutValue)
        {
            watchPrimaryValue = WatchPrimaryValue;
            tokenValue = TokenValue;
            scopeNameValue = ScopeNameValue;
            collectionNameValue = CollectionNameValue;
            queryContext = QueryContext;
            timeoutValue = TimeoutValue;
        }

        public ReadOnly AsReadOnly()
        {
            this.Deconstruct(out bool watchPrimaryValue, out CancellationToken tokenValue, out string? scopeNameValue, out string? collectionNameValue, out string? queryContext, out TimeSpan timeoutValue);
            return new ReadOnly(watchPrimaryValue, tokenValue, scopeNameValue, collectionNameValue, queryContext, timeoutValue);
        }

        public record ReadOnly(
            bool WatchPrimaryValue,
            CancellationToken TokenValue,
            string? ScopeNameValue,
            string? CollectionNameValue,
            string? QueryContext,
            TimeSpan TimeoutValue);
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

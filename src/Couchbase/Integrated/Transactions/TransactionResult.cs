#if NET5_0_OR_GREATER
#nullable enable
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Couchbase.Integrated.Transactions
{
    /// <summary>
    /// The result of a transaction.
    /// </summary>
    internal class TransactionResult
    {
        /// <summary>
        /// Gets the automatically-generated ID of this transaction.
        /// </summary>
        public string? TransactionId { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the transaction completed to the point of unstaging its results, meaning it finished successfully.
        /// </summary>
        public bool UnstagingComplete { get; internal set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return JObject.FromObject(this).ToString();
        }

        /// <summary>
        /// Gets the logs associated with this transaction.
        /// </summary>
        public IEnumerable<string> Logs { get; internal set; } = Enumerable.Empty<string>();
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

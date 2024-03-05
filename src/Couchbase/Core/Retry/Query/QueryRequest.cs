using System;
using Couchbase.Core.Diagnostics.Metrics;
using Couchbase.Query;

namespace Couchbase.Core.Retry.Query
{
    internal class QueryRequest : RequestBase
    {
        //specific to query
        public QueryOptions Options { get; set; }

        public override bool Idempotent => Options.IsReadOnly;

        public sealed override void StopRecording()
        {
            if (Stopwatch != null)
            {
                Stopwatch.Stop();
                MetricTracker.N1Ql.TrackOperation(this, Stopwatch.Elapsed);
            }
        }
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

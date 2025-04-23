using System;
using System.Collections.Generic;
using System.Linq;
using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.Query;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Core.RateLimiting;
using Couchbase.Core.Utils;

namespace Couchbase.Query
{
    internal static class QueryResultExtensions
    {
        private static readonly List<QueryStatus> StaleStatuses = new List<QueryStatus>
        {
            QueryStatus.Fatal,
            QueryStatus.Timeout,
            QueryStatus.Errors
        };

        private static readonly List<int> StaleErrorCodes = new List<int>
        {
            (int) ErrorPrepared.PreparedStatementNotFound,
            (int) ErrorPrepared.Unrecognized,
            (int) ErrorPrepared.UnableToDecode,
            (int) ErrorPrepared.Generic,
            (int) ErrorPrepared.IndexNotFound
        };

        private static readonly List<int> PreparedErrorCodes = new List<int>
        {
            4040,
            4050,
            4060,
            4070,
            4080,
            4090
        };

        /// <summary>
        /// Determines whether a prepared query's plan stale.
        /// </summary>
        /// <param name="queryResult">The N1QL query result.</param>
        /// <returns>
        ///   <c>true</c> if query plan is stale; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsQueryPlanStale<T>(this IQueryResult<T> queryResult)
        {
            return StaleStatuses.Contains(queryResult.MetaData.Status) &&
                   queryResult.Errors.Any(error => StaleErrorCodes.Contains(error.Code));
        }

        /// <summary>
        /// Create the appropriate Exception for an error context
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="result">Result</param>
        /// <param name="context">Error context</param>
        /// <returns>Exception</returns>
        public static CouchbaseException CreateExceptionForError<T>(this IQueryResult<T> result, QueryErrorContext context)
        {
            foreach (var error in result.Errors)
            {
                if (error.Code == 3000) return new ParsingFailureException(context);

                if (PreparedErrorCodes.Contains(error.Code)) return new PreparedStatementException(context);

                if (error.Code == 4300 && error.Message.Contains("index", StringComparison.OrdinalIgnoreCase) &&
                    error.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    return new IndexExistsException(context);

                if (error.Code >= 4000 && error.Code < 5000) return new PlanningFailureException(context);

                if (error.Code == 12004 || error.Code == 12016 ||
                    (error.Code == 5000 && error.Message.Contains("index", StringComparison.OrdinalIgnoreCase) &&
                    error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)) ||
                    (error.Code == 5000 && error.Message.Contains("does not exist")))
                    return new IndexNotFoundException(context);

                if (error.Code == 5000 && error.Message.Contains("index", StringComparison.OrdinalIgnoreCase) &&
                    error.Message.Contains("already exist", StringComparison.OrdinalIgnoreCase))
                    return new IndexExistsException(context);

                if (error.Code >= 5000 && error.Code < 6000) return new InternalServerFailureException(context);

                if (error.Code == 12009)
                {
                    if (error.Message.Contains("CAS mismatch", StringComparison.OrdinalIgnoreCase) || error.Reason.Code == 12033)
                    {
                        return new CasMismatchException(context);
                    }
                    if (error.Reason.Code == 17014)
                    {
                        return new DocumentNotFoundException(context);
                    }
                    if (error.Reason.Code == 17012)
                    {
                        return new DocumentExistsException(context);
                    }

                    return new DmlFailureException(context);
                }

                if (error.Code >= 10000 && error.Code < 11000 || error.Code == 13014)
                    return new AuthenticationFailureException(context);

                if (error.Code >= 12000 && error.Code < 13000 || error.Code >= 14000 && error.Code < 15000)
                    return new IndexFailureException(context);

                //Rate Limiting Errors
                if (error.Code == 1191)
                    return new RateLimitedException(RateLimitedReason.RequestRateLimitReached, context);
                if (error.Code == 1192)
                    return new RateLimitedException(RateLimitedReason.ConcurrentRequestLimitReached, context);
                if (error.Code == 1193)
                    return new RateLimitedException(RateLimitedReason.NetworkIngressRateLimitReached, context);
                if (error.Code == 1194)
                    return new RateLimitedException(RateLimitedReason.NetworkEgressRateLimitReached, context);

                //query_context was not included in the request and the server requires it
                if (error.Code == 1197) return new QueryContextMissingException(context);

                if (error.Code == 1080) return new UnambiguousTimeoutException("Query timed out while streaming/receiving rows.", context);

                //query_context *was* included in the request, but the server doesn't support it
                if (error.Code == 1065 && error.Message.Contains("query_context"))
                    return new FeatureNotAvailableException("query_context not available on this server version");

                if (error.Code == 2120 && error.Message.Contains("Failure to authenticate user"))
                {
                    return new AuthenticationFailureException(context);
                }
            }

            return new CouchbaseException(context);
        }

        /// <summary>
        /// Create/throw the appropriate Exception given an error context
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="result">Result</param>
        /// <param name="context">Error context</param>
        /// <returns></returns>
        public static Exception ThrowExceptionOnError<T>(this IQueryResult<T> result, QueryErrorContext context)
        {
            throw CreateExceptionForError(result, context);
        }

        public static bool InternalFailure<T>(this IQueryResult<T> result, out bool isRetryable)
        {
            isRetryable = false;
            foreach (var error in result.Errors)
            {
                if (error.Code >= 5000 && error.Code < 6000)
                {
                    isRetryable = (error.Message != null
                                   && error.Message.Contains(QueryClient.Error5000MsgQueryPortIndexNotFound));
                    return true;
                }
            }

            return false;
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
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

#endregion

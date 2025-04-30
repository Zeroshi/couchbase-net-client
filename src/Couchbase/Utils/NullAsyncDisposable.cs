using System;
using System.Threading.Tasks;

namespace Couchbase.Utils
{
    /// <summary>
    /// Implementation of <see cref="IAsyncDisposable"/> which does nothing.
    /// </summary>
    internal sealed class NullAsyncDisposable : IAsyncDisposable
    {
        /// <summary>
        /// Reusable static instance of <see cref="NullAsyncDisposable"/>.
        /// </summary>
        public static NullAsyncDisposable Instance { get; } = new NullAsyncDisposable();

        /// <inheritdoc />
        public ValueTask DisposeAsync() => default;
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

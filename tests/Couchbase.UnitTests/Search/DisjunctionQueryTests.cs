using System;
using Couchbase.Search.Queries.Compound;
using Couchbase.Search.Queries.Simple;
using Newtonsoft.Json;
using Xunit;

namespace Couchbase.UnitTests.Search
{
    public class DisjunctionQueryTests
    {
        [Fact]
        public void Boost_ReturnsDisjunctionQuery()
        {
            var query = new DisjunctionQuery().Boost(2.2);

            Assert.IsType<DisjunctionQuery>(query);
        }

        [Fact]
        public void Boost_WhenBoostIsLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            var query = new DisjunctionQuery();

            Assert.Throws<ArgumentOutOfRangeException>(() => query.Boost(-.1));
        }

        [Fact]
        public void Export_ReturnsValidJson()
        {
            var query = new DisjunctionQuery(
                new TermQuery("hotel").Field("type")
            );

            var result = query.Export().ToString(Formatting.None);

            var expected = JsonConvert.SerializeObject(new
            {
                disjuncts = new[]
                {
                    new
                    {
                        term = "hotel",
                        field = "type"
                    }
                }
            }, Formatting.None);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Can_create_disjunction_that_includes_query_with_boost()
        {
            new DisjunctionQuery(
                new MatchQuery("term1").Field("field1").Boost(2.0)
            );
        }

        [Fact]
        public void Can_add_query_with_boost()
        {
            new DisjunctionQuery()
                .Or(new MatchQuery("term1").Field("field1").Boost(2.0));
        }

        [Fact]
        public void Min_WhenMinIsLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            var query = new DisjunctionQuery();

            Assert.Throws<ArgumentOutOfRangeException>(() => query.Min(-1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Can_create_disjunction_that_includes_query_with_min_bigger_than_or_equal_to_zero(int min)
        {
            var query = new DisjunctionQuery().Min(min);

            Assert.IsType<DisjunctionQuery>(query);
        }
    }
}

#region [ License information          ]

/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2015 Couchbase, Inc.
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

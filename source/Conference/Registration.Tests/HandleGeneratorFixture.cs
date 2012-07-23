// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.Tests
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading.Tasks;
    using Conference.Common.Utils;
    using Xunit;

    public class HandleGeneratorFixture
    {
        [Fact]
        public void when_generating_handle_then_generates_requested_length()
        {
            var handle = HandleGenerator.Generate(5);

            Assert.Equal(5, handle.Length);
        }

        [Fact]
        public void when_generating_handles_then_generates_different_values()
        {
            Assert.NotEqual(HandleGenerator.Generate(5), HandleGenerator.Generate(5));
        }

        [Fact]
        public void is_thread_safe()
        {
            var list = new ConcurrentBag<string>();
            Parallel.For(0, 10000, i => list.Add(HandleGenerator.Generate(6)));

            Assert.Equal(10000, list.Count);
        }

        [Fact]
        public void should_generate_distinct_handles()
        {
            var list = new ConcurrentBag<string>();
            Parallel.For(0, 10000, i => list.Add(HandleGenerator.Generate(100)));

            Assert.Equal(10000, list.Distinct().Count());
        }
    }
}

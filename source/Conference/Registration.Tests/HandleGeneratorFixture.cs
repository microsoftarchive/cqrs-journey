// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.Tests
{
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
    }
}

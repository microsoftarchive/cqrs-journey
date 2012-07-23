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

using Xunit;

namespace Registration.Tests
{
    public class PersonalInfoFixture
    {
        [Fact]
        public void when_comparing_to_null_object_then_equals_false()
        {
            Assert.False(new PersonalInfo().Equals((object)null));
        }

        [Fact]
        public void when_comparing_to_null_then_equals_false()
        {
            Assert.False(new PersonalInfo().Equals((PersonalInfo)null));
        }

        [Fact]
        public void when_comparing_value_equal_then_returns_true()
        {
            Assert.Equal(
                new PersonalInfo { Email = "test@contoso.com" },
                new PersonalInfo { Email = "test@contoso.com" });

            Assert.Equal(
                new PersonalInfo { FirstName = "test@contoso.com" },
                new PersonalInfo { FirstName = "test@contoso.com" });

            Assert.Equal(
                new PersonalInfo { LastName = "test@contoso.com" },
                new PersonalInfo { LastName = "test@contoso.com" });

            Assert.Equal(
                new PersonalInfo { Email = "test@contoso.com", FirstName = "test" },
                new PersonalInfo { Email = "test@contoso.com", FirstName = "test" });

            Assert.Equal(
                new PersonalInfo { Email = "test@contoso.com", FirstName = "test", LastName = "one" },
                new PersonalInfo { Email = "test@contoso.com", FirstName = "test", LastName = "one" });
        }

        [Fact]
        public void when_comparing_with_operator_overload_then_succeeds()
        {
            Assert.True(
                new PersonalInfo { Email = "test@contoso.com" } == new PersonalInfo { Email = "test@contoso.com" });
            Assert.True(
                new PersonalInfo { Email = "test@contoso.com" } != new PersonalInfo { Email = "hello@world.com" });
        }

        [Fact]
        public void when_comparing_to_null_with_operator_overload_then_succeeds()
        {
            Assert.True(((PersonalInfo)null) == null);
            Assert.True(new PersonalInfo() != null);
            Assert.True(null != new PersonalInfo());
        }
    }
}

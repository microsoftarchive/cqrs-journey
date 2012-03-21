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

namespace Azure.Tests
{
    using System.Xml;
    using Azure.Messaging;
    using Xunit;

    public class given_a_messaging_settings_file
    {
        [Fact]
        public void when_read_from_file_then_succeeds()
        {
            var settings = MessagingSettings.Read("Settings.Template.xml");

            Assert.NotNull(settings);
        }

        [Fact]
        public void when_read_from_reader_then_succeeds()
        {
            using (var reader = XmlReader.Create("Settings.Template.xml"))
            {
                var settings = MessagingSettings.Read(reader);

                Assert.NotNull(settings);
            }
        }
    }
}

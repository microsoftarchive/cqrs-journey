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

namespace Infrastructure.Azure.Tests
{
    using System;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Xunit;

    public class given_a_messaging_settings_file
    {
        [Fact]
        public void when_reading_service_bus_from_file_then_succeeds()
        {
            var settings = InfrastructureSettings.Read("Settings.Template.xml").ServiceBus;

            Assert.NotNull(settings);
            Assert.Equal(4, settings.Topics.Count);
            Assert.Equal(3, settings.Topics[0].Subscriptions.Count);
        }

        [Fact]
        public void when_reading_topic_settings_then_sets_default_value_from_schema()
        {
            // Setup XSD validation so that we can load an XDocument with PSVI information
            var schema = XmlSchema.Read(typeof(InfrastructureSettings).Assembly.GetManifestResourceStream("Infrastructure.Azure.Settings.xsd"), null);
            var readerSettings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            readerSettings.Schemas.Add(schema);
            readerSettings.Schemas.Compile();

            using (var reader = XmlReader.Create("Settings.Template.xml", readerSettings))
            {
                var doc = XDocument.Load(reader);
                // Even if the attribute is not in the XML file, we can access the 
                // attribute because the XSD validation is adding the default value 
                // post validation.
                var defaultValue = doc.Root.Descendants(
                    XNamespace.Get(InfrastructureSettings.XmlNamespace) + "Topic")
                    .Skip(1)
                    .First()
                    .Attribute("DuplicateDetectionHistoryTimeWindow")
                    .Value;

                var settings = InfrastructureSettings.Read("Settings.Template.xml").ServiceBus;

                Assert.Equal(
                    TimeSpan.Parse(defaultValue),
                    settings.Topics[1].DuplicateDetectionHistoryTimeWindow);
            }
        }

        [Fact]
        public void when_reading_eventsourcing_settings_from_file_then_succeeds()
        {
            var settings = InfrastructureSettings.Read("Settings.Template.xml").EventSourcing;

            Assert.NotNull(settings);
        }

        [Fact]
        public void when_reading_eventlog_settings_from_file_then_succeeds()
        {
            var settings = InfrastructureSettings.Read("Settings.Template.xml").MessageLog;

            Assert.NotNull(settings);
        }
    }
}

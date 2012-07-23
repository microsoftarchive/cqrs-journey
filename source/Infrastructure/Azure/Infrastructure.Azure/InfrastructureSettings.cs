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

namespace Infrastructure.Azure
{
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using Infrastructure.Azure.BlobStorage;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Azure.Messaging;

    /// <summary>
    /// Simple settings class to configure the connection to Windows Azure services.
    /// </summary>
    [XmlRoot("InfrastructureSettings", Namespace = XmlNamespace)]
    public class InfrastructureSettings
    {
        public const string XmlNamespace = @"urn:microsoft-patterns-and-practices-cqrsjourney";

        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(InfrastructureSettings));
        private static readonly XmlReaderSettings readerSettings;

        static InfrastructureSettings()
        {
            var schema = XmlSchema.Read(typeof(InfrastructureSettings).Assembly.GetManifestResourceStream("Infrastructure.Azure.Settings.xsd"), null);
            readerSettings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            readerSettings.Schemas.Add(schema);
        }

        /// <summary>
        /// Reads the settings from the specified file.
        /// </summary>
        public static InfrastructureSettings Read(string file)
        {
            using (var reader = XmlReader.Create(file, readerSettings))
            {
                return (InfrastructureSettings)serializer.Deserialize(reader);
            }
        }

        public ServiceBusSettings ServiceBus { get; set; }
        public EventSourcingSettings EventSourcing { get; set; }
        public MessageLogSettings MessageLog { get; set; }
        public BlobStorageSettings BlobStorage { get; set; }
    }
}

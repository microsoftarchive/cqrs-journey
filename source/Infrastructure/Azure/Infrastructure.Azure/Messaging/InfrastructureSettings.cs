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

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.Xml.XPath;

    /// <summary>
    /// Simple settings class to configure the connection to Azure.
    /// </summary>
    public class InfrastructureSettings
    {
        private static readonly XmlSerializer messagingSerializer = new XmlSerializer(typeof(MessagingSettings));
        private static readonly XmlSerializer eventSourcingSerializer = new XmlSerializer(typeof(EventSourcingSettings));

        /// <summary>
        /// Reads the messaging settings from the specified file.
        /// </summary>
        public static MessagingSettings ReadMessaging(string file)
        {
            var doc = XDocument.Load(file);
            //var settings = doc.XPathSelectElement(string.Format("/InfrastructureSettings/Messaging/ServiceBus[@name='{0}']", configurationName));
            var settings = doc.XPathSelectElement("/InfrastructureSettings/Messaging");
            if (settings == null)
            {
                throw new ArgumentException("file");
            }

            using (var reader = settings.CreateReader())
            {
                return (MessagingSettings)messagingSerializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Reads the event sourcing settings from the specified file.
        /// </summary>
        public static EventSourcingSettings ReadEventSourcing(string file)
        {
            var doc = XDocument.Load(file);
            var settings = doc.XPathSelectElement("/InfrastructureSettings/EventSourcing");
            if (settings == null)
            {
                throw new ArgumentException("file");
            }

            using (var reader = settings.CreateReader())
            {
                return (EventSourcingSettings)eventSourcingSerializer.Deserialize(reader);
            }
        }
    }
}

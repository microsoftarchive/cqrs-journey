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

using Common.Test;

namespace Infrastructure.Azure.IntegrationTests.TopicSenderIntegration
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    public class given_a_topic_sender : IDisposable
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(MessagingSettings));
        private MessagingSettings settings;
        private string topic = Guid.NewGuid().ToString();

        public given_a_topic_sender()
        {
            using (var file = File.OpenRead("Settings.xml"))
            {
                this.settings = (MessagingSettings)serializer.Deserialize(file);
            }
        }

        public void Dispose()
        {
            this.settings.TryDeleteTopic(this.topic);
        }

        [Fact]
        public void when_sending_message_then_succeeds()
        {
            var sender = new TopicSender(this.settings, this.topic);

            sender.Send(new BrokeredMessage(Guid.NewGuid()));
        }

        [Fact]
        public void when_sending_message_batch_then_succeeds()
        {
            var sender = new TopicSender(this.settings, this.topic);

            sender.Send(new[] { new BrokeredMessage(Guid.NewGuid()), new BrokeredMessage(Guid.NewGuid()) });
        }
    }
}

// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Azure
{
    using System.Collections.Generic;
    using System.IO;
    using Azure.Messaging;
    using Common;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// An event bus that sends serialized object payloads through a <see cref="IMessageSender"/>.
    /// </summary>
    public class EventBus : IEventBus
    {
        private IMessageSender sender;
        private ISerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBus"/> class.
        /// </summary>
        public EventBus(IMessageSender sender, ISerializer serializer)
        {
            this.sender = sender;
            this.serializer = serializer;
        }

        /// <summary>
        /// Sends the specified command.
        /// </summary>
        public void Publish(IEvent @event)
        {
            var stream = new MemoryStream();
            this.serializer.Serialize(stream, @event);
            stream.Position = 0;

            var message = new BrokeredMessage(stream, true);
            message.Properties["Type"] = @event.GetType().FullName;
            // TODO: should we use Path.GetFileNameWithoutExtension(message.GetType().Assembly.ManifestModule.FullyQualifiedName) instead? (partial trust?)
            message.Properties["Assembly"] = @event.GetType().Assembly.GetName().Name;

            this.sender.Send(message);
        }

        public void Publish(IEnumerable<IEvent> events)
        {
            // TODO: batch/transactional sending? Is it just wrapping with a TransactionScope?
            foreach (var @event in events)
            {
                this.Publish(@event);
            }
        }
    }
}

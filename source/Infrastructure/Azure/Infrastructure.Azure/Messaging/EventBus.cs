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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// An event bus that sends serialized object payloads through a <see cref="IMessageSender"/>.
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly IMessageSender sender;
        private readonly IMetadataProvider metadata;
        private readonly ITextSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBus"/> class.
        /// </summary>
        /// <param name="serializer">The serializer to use for the message body.</param>
        public EventBus(IMessageSender sender, IMetadataProvider metadata, ITextSerializer serializer)
        {
            this.sender = sender;
            this.metadata = metadata;
            this.serializer = serializer;
        }

        /// <summary>
        /// Sends the specified event.
        /// </summary>
        public void Publish(IEvent @event)
        {
            this.sender.SendAsync(() => BuildMessage(@event));
        }

        /// <summary>
        /// Publishes the specified events.
        /// </summary>
        public void Publish(IEnumerable<IEvent> events)
        {
            var messageFactories = events.Select<IEvent, Func<BrokeredMessage>>(e => () => this.BuildMessage(e));

            this.sender.SendAsync(messageFactories);
        }

        private BrokeredMessage BuildMessage(IEvent @event)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            this.serializer.Serialize(writer, @event);
            stream.Position = 0;

            var message = new BrokeredMessage(stream, true);

            foreach (var pair in this.metadata.GetMetadata(@event))
            {
                message.Properties[pair.Key] = pair.Value;
            }

            message.Properties["SourceId"] = @event.SourceId;

            return message;
        }
    }
}

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

namespace Infrastructure.Azure.Messaging
{
    using System.Collections.Generic;
    using System.IO;

    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// An event bus that sends serialized object payloads through a <see cref="IMessageSender"/>.
    /// </summary>
    /// <remarks>Note that <see cref="Infrastructure.EventSourcing.IEventSourced"/> entities persisted through the <see cref="IEventSourcedRepository{T}"/>
    /// do not use the <see cref="IEventBus"/>, but has its own event publishing mechanism.</remarks>
    public class EventBus : IEventBus
    {
        private readonly IMessageSender sender;
        private readonly IMetadataProvider metadataProvider;
        private readonly ITextSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBus"/> class.
        /// </summary>
        /// <param name="serializer">The serializer to use for the message body.</param>
        public EventBus(IMessageSender sender, IMetadataProvider metadataProvider, ITextSerializer serializer)
        {
            this.sender = sender;
            this.metadataProvider = metadataProvider;
            this.serializer = serializer;
        }

        /// <summary>
        /// Sends the specified event.
        /// </summary>
        public void Publish(Envelope<IEvent> @event)
        {
            this.sender.Send(() => BuildMessage(@event));
        }

        /// <summary>
        /// Publishes the specified events.
        /// </summary>
        public void Publish(IEnumerable<Envelope<IEvent>> events)
        {
            foreach (var @event in events)
            {
                this.Publish(@event);
            }
        }

        private BrokeredMessage BuildMessage(Envelope<IEvent> envelope)
        {
            var @event = envelope.Body;

            var stream = new MemoryStream();
            try
            {
                var writer = new StreamWriter(stream);
                this.serializer.Serialize(writer, @event);
                stream.Position = 0;

                var message = new BrokeredMessage(stream, true);

                message.SessionId = @event.SourceId.ToString();

                if (!string.IsNullOrWhiteSpace(envelope.MessageId))
                {
                    message.MessageId = envelope.MessageId;
                }

                if (!string.IsNullOrWhiteSpace(envelope.CorrelationId))
                {
                    message.CorrelationId = envelope.CorrelationId;
                }

                var metadata = this.metadataProvider.GetMetadata(@event);
                if (metadata != null)
                {
                    foreach (var pair in metadata)
                    {
                        message.Properties[pair.Key] = pair.Value;
                    }
                }

                return message;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
    }
}

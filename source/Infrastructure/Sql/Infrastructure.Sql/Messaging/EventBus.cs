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

namespace Infrastructure.Sql.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;

    /// <summary>
    /// An event bus that sends serialized object payloads through a <see cref="IMessageSender"/>.
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly IMessageSender sender;
        private readonly ITextSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBus"/> class.
        /// </summary>
        /// <param name="receiver">The receiver to use. If the receiver is <see cref="IDisposable"/>, it will be disposed when the processor is 
        /// disposed.</param>
        /// <param name="serializer">The serializer to use for the message body.</param>
        public EventBus(IMessageSender sender, ITextSerializer serializer)
        {
            this.sender = sender;
            this.serializer = serializer;
        }

        /// <summary>
        /// Sends the specified event.
        /// </summary>
        public void Publish(IEvent @event)
        {
            var message = BuildMessage(@event, null);

            this.sender.Send(message);
        }

        /// <summary>
        /// Publishes the specified events.
        /// </summary>
        public void Publish(IEnumerable<IEvent> events)
        {
            var messages = events.Select(e => BuildMessage(e, null));

            this.sender.Send(messages);
        }

        /// <summary>
        /// Publishes the specified events with a correlation id.
        /// </summary>
        public void Publish(IEnumerable<IEvent> events, string correlationId)
        {
            var messages = events.Select(e => BuildMessage(e, correlationId));

            this.sender.Send(messages);
        }

        private Message BuildMessage(IEvent @event, string correlationId)
        {
            using (var payloadWriter = new StringWriter())
            {
                this.serializer.Serialize(payloadWriter, @event);
                return new Message(payloadWriter.ToString(), correlationId: correlationId);
            }
        }
    }
}

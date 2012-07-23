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

namespace Infrastructure.Sql.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;

    /// <summary>
    /// This is an extremely basic implementation of <see cref="ICommandBus"/> that is used only for running the sample
    /// application without the dependency to the Windows Azure Service Bus when using the DebugLocal solution configuration.
    /// It should not be used in production systems.
    /// </summary>
    public class CommandBus : ICommandBus
    {
        private readonly IMessageSender sender;
        private readonly ITextSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandBus"/> class.
        /// </summary>
        /// <param name="serializer">The serializer to use for the message body.</param>
        public CommandBus(IMessageSender sender, ITextSerializer serializer)
        {
            this.sender = sender;
            this.serializer = serializer;
        }

        /// <summary>
        /// Sends the specified command.
        /// </summary>
        public void Send(Envelope<ICommand> command)
        {
            var message = BuildMessage(command);

            this.sender.Send(message);
        }

        public void Send(IEnumerable<Envelope<ICommand>> commands)
        {
            var messages = commands.Select(command => BuildMessage(command));

            this.sender.Send(messages);
        }

        private Message BuildMessage(Envelope<ICommand> command)
        {
            // TODO: should use the Command ID as a unique constraint when storing it.
            using (var payloadWriter = new StringWriter())
            {
                this.serializer.Serialize(payloadWriter, command.Body);
                return new Message(payloadWriter.ToString(), command.Delay != TimeSpan.Zero ? (DateTime?)DateTime.UtcNow.Add(command.Delay) : null, command.CorrelationId);
            }
        }
    }
}

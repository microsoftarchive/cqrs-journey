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

namespace Infrastructure.Azure.Messaging.Handling
{
    using System;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;

    /// <summary>
    /// Processes incoming commands from the bus and routes them to the appropriate 
    /// handlers.
    /// </summary>
    public class CommandProcessor : MessageProcessor, ICommandHandlerRegistry
    {
        private readonly CommandDispatcher commandDispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandProcessor"/> class.
        /// </summary>
        /// <param name="receiver">The receiver to use. If the receiver is <see cref="IDisposable"/>, it will be disposed when the processor is 
        /// disposed.</param>
        /// <param name="serializer">The serializer to use for the message body.</param>
        public CommandProcessor(IMessageReceiver receiver, ITextSerializer serializer)
            : base(receiver, serializer)
        {
            this.commandDispatcher = new CommandDispatcher();
        }

        /// <summary>
        /// Registers the specified command handler.
        /// </summary>
        public void Register(ICommandHandler commandHandler)
        {
            this.commandDispatcher.Register(commandHandler);
        }

        /// <summary>
        /// Processes the message by calling the registered handler.
        /// </summary>
        protected override void ProcessMessage(string traceIdentifier, object payload, string messageId, string correlationId)
        {
            this.commandDispatcher.ProcessMessage(traceIdentifier, (ICommand)payload, messageId, correlationId);
        }
    }
}

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

namespace Azure
{
    using System.Collections.Generic;
    using System.Linq;
    using Azure.Messaging;
    using Common;

    /// <summary>
    /// Processes incoming commands from the bus and routes them to the appropriate 
    /// handlers.
    /// </summary>
    public class CommandProcessor : MessageProcessor
    {
        private List<ICommandHandler> handlers = new List<ICommandHandler>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandProcessor"/> class.
        /// </summary>
        /// <param name="receiver">The receiver to use. If the receiver is <see cref="IDisposable"/>, it will be disposed when the processor is 
        /// disposed.</param>
        /// <param name="serializer">The serializer to use for the message body.</param>
        public CommandProcessor(IMessageReceiver receiver, ISerializer serializer)
            : base(receiver, serializer)
        {
        }

        /// <summary>
        /// Registers the specified command handler.
        /// </summary>
        public void Register(ICommandHandler commandHandler)
        {
            this.handlers.Add(commandHandler);
        }

        /// <summary>
        /// Processes the message by calling the registered handler.
        /// </summary>
        protected override void ProcessMessage(object payload)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(payload.GetType());

            // TODO: throw if more than one handler here? This would never assure us 
            // that there aren't duplicate handlers in multiple processes, so it's kinda 
            // pointless here. Also, what are we supposed to do if we throw? DeadLetter 
            // the message? Kill the process? TBD.
            foreach (dynamic handler in this.handlers
                .Where(x => handlerType.IsAssignableFrom(x.GetType())))
            {
                handler.Handle((dynamic)payload);
            }
        }

    }
}

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

        public CommandProcessor(IMessageReceiver receiver, ISerializer serializer)
            : base(receiver, serializer)
        {
        }

        public void Register(ICommandHandler commandHandler)
        {
            this.handlers.Add(commandHandler);
        }

        protected override void ProcessMessage(object payload)
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(payload.GetType());

            // TODO: throw if more than one handler here?
            foreach (dynamic handler in this.handlers
                .Where(x => handlerType.IsAssignableFrom(x.GetType())))
            {
                handler.Handle((dynamic)payload);
            }
        }

    }
}

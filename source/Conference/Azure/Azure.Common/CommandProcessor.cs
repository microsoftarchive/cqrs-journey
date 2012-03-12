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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Azure.Messaging;
    using Common;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Processes incoming messages from the bus and routes them to the appropriate 
    /// handlers.
    /// </summary>
    public class CommandProcessor : IListener, IDisposable
    {
        private List<ICommandHandler> handlers = new List<ICommandHandler>();
        private IMessageReceiver receiver;
        private ISerializer serializer;

        public CommandProcessor(IMessageReceiver receiver, ISerializer serializer)
        {
            this.receiver = receiver;
            this.serializer = serializer;

            this.receiver.MessageReceived += this.OnMessageReceived;
        }

        public void Register(ICommandHandler commandHandler)
        {
            this.handlers.Add(commandHandler);
        }

        public void Start()
        {
            this.receiver.Start();
        }

        public void Stop()
        {
            this.receiver.Stop();
        }

        public void Dispose()
        {
            var disposable = this.receiver as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        private void OnMessageReceived(object sender, BrokeredMessageEventArgs args)
        {
            // Grab type information from message properties.

            object typeValue = null;
            object assemblyValue = null;

            if (args.Message.Properties.TryGetValue("Type", out typeValue) &&
                args.Message.Properties.TryGetValue("Assembly", out assemblyValue))
            {
                var typeName = (string)args.Message.Properties["Type"];
                var assemblyName = (string)args.Message.Properties["Assembly"];

                var type = Type.GetType(typeName);

                if (type != null)
                {
                    ReadMessage(args.Message, type);
                    return;
                }

                var assembly = Assembly.LoadWithPartialName(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        ReadMessage(args.Message, type);
                        return;
                    }
                }
            }

            // TODO: if we got here, it's 'cause we couldn't read the type.
            // Should we throw? Log? Ignore?
            args.Message.Async(args.Message.BeginAbandon, args.Message.EndAbandon);
        }

        private void ReadMessage(BrokeredMessage message, Type commandType)
        {
            using (var stream = message.GetBody<Stream>())
            {
                var command = this.serializer.Deserialize(stream, commandType);
                var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

                foreach (dynamic handler in this.handlers
                    .Where(x => handlerType.IsAssignableFrom(x.GetType())))
                {
                    handler.Handle((dynamic)command);
                }
            }

            message.Async(message.BeginComplete, message.EndComplete);
        }

    }
}

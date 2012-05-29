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

namespace Infrastructure.Sql.Messaging.Handling
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.Messaging;

    /// <summary>
    /// Processes incoming events from the bus and routes them to the appropriate 
    /// handlers.
    /// </summary>
    public class EventProcessor : MessageProcessor, IEventHandlerRegistry
    {
        // A simpler list just works. We don't care about two handlers for the same event 
        // type, etc.
        private List<IEventHandler> handlers = new List<IEventHandler>();

        public EventProcessor(IMessageReceiver receiver, ITextSerializer serializer)
            : base(receiver, serializer)
        {
        }

        public void Register(IEventHandler eventHandler)
        {
            this.handlers.Add(eventHandler);
        }

        protected override void ProcessMessage(object payload)
        {
            var handlerType = typeof(IEventHandler<>).MakeGenericType(payload.GetType());

            foreach (dynamic handler in this.handlers
                .Where(x => handlerType.IsAssignableFrom(x.GetType()) || CanHandle(x.GetType(), payload.GetType())))
            {
                Trace.WriteLine("-- Handled by " + ((object)handler).GetType().FullName);
                handler.Handle((dynamic)payload);
            }
        }

        /// <summary>
        /// Searches for all <see cref="IEventHandler{TEvent}"/> interfaces on the 
        /// <paramref name="handlerType"/>, gets the generic parameter on them and 
        /// determines if the payload type is a derived class of the parameter. 
        /// This makes it possible for a handler to handle events of a more 
        /// generic type.
        /// </summary>
        private bool CanHandle(Type handlerType, Type payloadType)
        {
            return handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .Select(i => i.GetGenericArguments()[0])
                .Where(t => t.IsAssignableFrom(payloadType))
                .Any();
        }
    }
}

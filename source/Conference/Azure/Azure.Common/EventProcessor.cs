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
    /// Processes incoming events from the bus and routes them to the appropriate 
    /// handlers.
    /// </summary>
    public class EventProcessor : MessageProcessor
    {
        // A simpler list just works. We don't care about two handlers for the same event 
        // type, etc.
        private List<IEventHandler> handlers = new List<IEventHandler>();

        public EventProcessor(IMessageReceiver receiver, ISerializer serializer)
            : base(receiver, serializer)
        {
        }

        public void Register(IEventHandler eventHandler)
        {
            this.handlers.Add(eventHandler);
        }

        protected override void ProcessMessage(object payload)
        {
            var handlerTypes = payload.GetType().GetInterfaces()
                .Select(iface => typeof(IEventHandler<>).MakeGenericType(iface))
                .Concat(new[] { typeof(IEventHandler<>).MakeGenericType(payload.GetType()) })
                .ToList();

            foreach (dynamic handler in this.handlers
                .Where(x => handlerTypes.Any(t => t.IsAssignableFrom(x.GetType()))))
            {
                handler.Handle((dynamic)payload);
            }
        }
    }
}

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
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;

    /// <summary>
    /// Processes incoming events from the bus and routes them to the appropriate 
    /// handlers.
    /// </summary>
    // TODO: now that we have just one handler per subscription, it doesn't make 
    // much sense to have this processor doing multi dispatch.
    public class EventProcessor : MessageProcessor, IEventHandlerRegistry
    {
        private readonly EventDispatcher eventDispatcher;

        public EventProcessor(IMessageReceiver receiver, ITextSerializer serializer)
            : base(receiver, serializer)
        {
            this.eventDispatcher = new EventDispatcher();
        }

        public void Register(IEventHandler eventHandler)
        {
            this.eventDispatcher.Register(eventHandler);
        }

        protected override void ProcessMessage(string traceIdentifier, object payload, string messageId, string correlationId)
        {
            var @event = payload as IEvent;
            if (@event != null)
            {
                this.eventDispatcher.DispatchMessage(@event, messageId, correlationId, traceIdentifier);
            }
        }
    }
}

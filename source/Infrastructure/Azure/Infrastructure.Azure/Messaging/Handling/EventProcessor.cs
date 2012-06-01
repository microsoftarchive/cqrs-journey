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

namespace Infrastructure.Azure.Messaging.Handling
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
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
        // A simpler list just works. We don't care about two handlers for the same event 
        // type, etc.
        private List<IEventHandler> handlers = new List<IEventHandler>();
        private Dictionary<Type, Action<string, object, string, string>> processMethods = new Dictionary<Type, Action<string, object, string, string>>();

        public EventProcessor(IMessageReceiver receiver, ITextSerializer serializer)
            : base(receiver, serializer)
        {
        }

        public void Register(IEventHandler eventHandler)
        {
            this.handlers.Add(eventHandler);
        }

        protected override void ProcessMessage(string traceIdentifier, object payload, string messageId, string correlationId)
        {
            var processMessage = this.GetProcessMessageDelegate(payload.GetType());

            processMessage(traceIdentifier, payload, messageId, correlationId);
        }

        private void DoProcessMessage<T>(string traceIdentifier, object @event, string messageId, string correlationId)
            where T : IEvent
        {
            var envelope = ReceiveEnvelope.Create<T>((T)@event, messageId, correlationId);

            foreach (var handler in this.handlers.Select(x => x as IEventHandler<T>).Where(x => x != null))
            {
                Trace.WriteLine("-- Handled by " + handler.GetType().FullName + traceIdentifier);
                handler.Handle(envelope.Body);
            }

            foreach (var envelopeHandler in this.handlers.Select(x => x as IEnvelopedEventHandler<T>).Where(x => x != null))
            {
                Trace.WriteLine("-- Handled with envelope by " + envelopeHandler.GetType().FullName + traceIdentifier);
                envelopeHandler.Handle(envelope);
            }
        }

        private Action<string, object, string, string> GetProcessMessageDelegate(Type eventType)
        {
            Action<string, object, string, string> action;

            if (!this.processMethods.TryGetValue(eventType, out action))
            {
                action = (Action<string, object, string, string>)
                    Delegate.CreateDelegate(
                        typeof(Action<string, object, string, string>),
                        this,
                        this.GetType().GetMethod("DoProcessMessage", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(eventType));
                this.processMethods[eventType] = action;
            }

            return action;
        }
    }
}

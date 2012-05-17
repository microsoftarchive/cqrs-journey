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

namespace Infrastructure.Messaging.Handling
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;

    public class MessageDispatcher
    {
        private Dictionary<Type, List<Tuple<Type, Action<IEvent>>>> handlersByMessageType;

        public MessageDispatcher(IEnumerable<IEventHandler> handlers)
        {
            this.handlersByMessageType =
                handlers
                    .SelectMany(h => this.BuildHandlerInvocations(h).Select(i => new { HandlerType = h.GetType(), EventType = i.Item1, Invocation = i.Item2 }))
                    .GroupBy(e => e.EventType)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => new Tuple<Type, Action<IEvent>>(e.HandlerType, e.Invocation)).ToList());
        }

        private IEnumerable<Tuple<Type, Action<IEvent>>> BuildHandlerInvocations(IEventHandler handler)
        {
            return handler.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    .Select(i => new { HandlerInterface = i, EventType = i.GetGenericArguments()[0] })
                    .Select(e => new Tuple<Type, Action<IEvent>>(e.EventType, this.BuildHandlerInvocation(handler, e.HandlerInterface, e.EventType)));
        }

        private Action<IEvent> BuildHandlerInvocation(IEventHandler handler, Type handlerType, Type messageType)
        {
            var parameter = Expression.Parameter(typeof(IEvent));
            var invocationExpression =
                Expression.Lambda(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(Expression.Constant(handler), handlerType),
                            handlerType.GetMethod("Handle"),
                            Expression.Convert(parameter, messageType))),
                    parameter);

            return (Action<IEvent>)invocationExpression.Compile();
        }

        public void DispatchMessages(IEnumerable<IEvent> messages)
        {
            foreach (var @event in messages)
            {
                DispatchMessage(@event);
            }
        }

        public void DispatchMessage(IEvent message)
        {
            Trace.WriteLine(BuildMessageDescription(message));

            List<Tuple<Type, Action<IEvent>>> handlers;
            if (this.handlersByMessageType.TryGetValue(message.GetType(), out handlers))
            {
                foreach (var handler in handlers)
                {
                    Trace.WriteLine("-- Handled by " + handler.Item1.FullName);
                    handler.Item2(message);
                }
            }
            else
            {
                Trace.WriteLine("-- Not handled");
            }
        }

        private string BuildMessageDescription(IEvent message)
        {
            var versionedEvent = message as IVersionedEvent;

            if (versionedEvent != null)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "Processing event of type {0} for source id {1} with version {2}.",
                    versionedEvent.GetType().Name,
                    versionedEvent.SourceId,
                    versionedEvent.Version);
            }
            else
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "Processing event of type {0} for source id {1}.",
                    message.GetType().Name,
                    message.SourceId);
            }
        }
    }
}
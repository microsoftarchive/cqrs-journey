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

namespace MigrationToV2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;

    public class EventReplayer
    {
        private Dictionary<Type, List<Tuple<Type, Action<IEvent>>>> handlersByEventType;

        public EventReplayer(IEnumerable<IEventHandler> handlers)
        {
            this.handlersByEventType =
                handlers
                    .SelectMany(
                        h =>
                            h.GetType().GetInterfaces()
                                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                                .Select(i => new { Handler = h, Interface = i, EventType = i.GetGenericArguments()[0] }))
                    .GroupBy(e => e.EventType)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => new Tuple<Type, Action<IEvent>>(e.Handler.GetType(), BuildHandlerInvocation(e.Handler, e.Interface, e.EventType))).ToList());
        }

        private Action<IEvent> BuildHandlerInvocation(IEventHandler handler, Type handlerType, Type eventType)
        {
            var parameter = Expression.Parameter(typeof(IEvent));
            var invocationExpression =
                Expression.Lambda(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(Expression.Constant(handler), handlerType),
                            handlerType.GetMethod("Handle"),
                            Expression.Convert(parameter, eventType))),
                    parameter);

            return (Action<IEvent>)invocationExpression.Compile();
        }

        public void ReplayEvents(IEnumerable<IEvent> events)
        {
            List<Tuple<Type, Action<IEvent>>> handlers;
            foreach (var @event in events)
            {
                Trace.WriteLine(BuildEventDescription(@event));
                if (this.handlersByEventType.TryGetValue(@event.GetType(), out handlers))
                {
                    foreach (var handler in handlers)
                    {
                        Trace.WriteLine("-- Handled by " + handler.Item1.FullName);
                        handler.Item2(@event);
                    }
                }
                else
                {
                    Trace.WriteLine("-- Not handled");
                }
            }
        }

        private string BuildEventDescription(IEvent @event)
        {
            var versionedEvent = @event as IVersionedEvent;

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
                    @event.GetType().Name,
                    @event.SourceId);
            }
        }
    }
}
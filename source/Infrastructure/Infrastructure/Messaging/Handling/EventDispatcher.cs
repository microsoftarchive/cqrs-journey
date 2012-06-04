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
    using System.Reflection;
    using Infrastructure.EventSourcing;

    public class EventDispatcher
    {
        private Dictionary<Type, List<Tuple<Type, Action<ReceiveEnvelope>>>> handlersByEventType;
        private Dictionary<Type, Action<IEvent, string, string, string>> dispatchersByEventType;

        public EventDispatcher()
        {
            this.handlersByEventType = new Dictionary<Type, List<Tuple<Type, Action<ReceiveEnvelope>>>>();
            this.dispatchersByEventType = new Dictionary<Type, Action<IEvent, string, string, string>>();
        }

        public EventDispatcher(IEnumerable<IEventHandler> handlers)
            : this()
        {
            foreach (var handler in handlers)
            {
                this.Register(handler);
            }
        }

        public void Register(IEventHandler handler)
        {
            var handlerType = handler.GetType();

            foreach (var invocationTuple in this.BuildHandlerInvocations(handler))
            {
                var envelopeType = typeof(ReceiveEnvelope<>).MakeGenericType(invocationTuple.Item1);

                List<Tuple<Type, Action<ReceiveEnvelope>>> invocations;
                if (!this.handlersByEventType.TryGetValue(invocationTuple.Item1, out invocations))
                {
                    invocations = new List<Tuple<Type, Action<ReceiveEnvelope>>>();
                    this.handlersByEventType[invocationTuple.Item1] = invocations;
                }
                invocations.Add(new Tuple<Type, Action<ReceiveEnvelope>>(handlerType, invocationTuple.Item2));

                if (!this.dispatchersByEventType.ContainsKey(invocationTuple.Item1))
                {
                    this.dispatchersByEventType[invocationTuple.Item1] = this.BuildDispatchInvocation(invocationTuple.Item1);
                }
            }
        }

        public void DispatchMessages(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                this.DispatchMessage(@event);
            }
        }

        public void DispatchMessage(IEvent @event)
        {
            this.DispatchMessage(@event, null, null, "");
        }

        public void DispatchMessage(IEvent @event, string messageId, string correlationId, string traceIdentifier)
        {
            Action<IEvent, string, string, string> dispatch;
            if (this.dispatchersByEventType.TryGetValue(@event.GetType(), out dispatch))
            {
                dispatch(@event, messageId, correlationId, traceIdentifier);
            }
        }

        private void DoDispatchMessage<T>(T @event, string messageId, string correlationId, string traceIdentifier)
            where T : IEvent
        {
            var envelope = ReceiveEnvelope.Create(@event, messageId, correlationId);

            List<Tuple<Type, Action<ReceiveEnvelope>>> handlers;
            if (this.handlersByEventType.TryGetValue(typeof(T), out handlers))
            {
                foreach (var handler in handlers)
                {
                    Trace.WriteLine("-- Handled by " + handler.Item1.FullName + traceIdentifier);
                    handler.Item2(envelope);
                }
            }
        }

        private IEnumerable<Tuple<Type, Action<ReceiveEnvelope>>> BuildHandlerInvocations(IEventHandler handler)
        {
            var interfaces = handler.GetType().GetInterfaces();

            var eventHandlerInvocations =
                interfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                    .Select(i => new { HandlerInterface = i, EventType = i.GetGenericArguments()[0] })
                    .Select(e => new Tuple<Type, Action<ReceiveEnvelope>>(e.EventType, this.BuildHandlerInvocation(handler, e.HandlerInterface, e.EventType)));

            var envelopedEventHandlerInvocations =
                interfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnvelopedEventHandler<>))
                    .Select(i => new { HandlerInterface = i, EventType = i.GetGenericArguments()[0] })
                    .Select(e => new Tuple<Type, Action<ReceiveEnvelope>>(e.EventType, this.BuildEnvelopeHandlerInvocation(handler, e.HandlerInterface, e.EventType)));

            return eventHandlerInvocations.Union(envelopedEventHandlerInvocations);
        }

        private Action<ReceiveEnvelope> BuildHandlerInvocation(IEventHandler handler, Type handlerType, Type messageType)
        {
            var envelopeType = typeof(ReceiveEnvelope<>).MakeGenericType(messageType);

            var parameter = Expression.Parameter(typeof(ReceiveEnvelope));
            var invocationExpression =
                Expression.Lambda(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(Expression.Constant(handler), handlerType),
                            handlerType.GetMethod("Handle"),
                            Expression.Property(Expression.Convert(parameter, envelopeType), "Body"))),
                    parameter);

            return (Action<ReceiveEnvelope>)invocationExpression.Compile();
        }

        private Action<ReceiveEnvelope> BuildEnvelopeHandlerInvocation(IEventHandler handler, Type handlerType, Type messageType)
        {
            var envelopeType = typeof(ReceiveEnvelope<>).MakeGenericType(messageType);

            var parameter = Expression.Parameter(typeof(ReceiveEnvelope));
            var invocationExpression =
                Expression.Lambda(
                    Expression.Block(
                        Expression.Call(
                            Expression.Convert(Expression.Constant(handler), handlerType),
                            handlerType.GetMethod("Handle"),
                            Expression.Convert(parameter, envelopeType))),
                    parameter);

            return (Action<ReceiveEnvelope>)invocationExpression.Compile();
        }

        private Action<IEvent, string, string, string> BuildDispatchInvocation(Type eventType)
        {
            var eventParameter = Expression.Parameter(typeof(IEvent));
            var messageIdParameter = Expression.Parameter(typeof(string));
            var correlationIdParameter = Expression.Parameter(typeof(string));
            var traceIdParameter = Expression.Parameter(typeof(string));

            var dispatchExpression =
                Expression.Lambda(
                    Expression.Block(
                        Expression.Call(
                            Expression.Constant(this),
                            this.GetType().GetMethod("DoDispatchMessage", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(eventType),
                            Expression.Convert(eventParameter, eventType),
                            messageIdParameter,
                            correlationIdParameter,
                            traceIdParameter)),
                    eventParameter,
                    messageIdParameter,
                    correlationIdParameter,
                    traceIdParameter);

            return (Action<IEvent, string, string, string>)dispatchExpression.Compile();
        }
    }
}
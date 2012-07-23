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

namespace Registration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Xunit;

    public class EventSourcingTestHelper<T> where T : IEventSourced
    {
        private ICommandHandler handler;
        private readonly RepositoryStub repository;
        private string expectedCorrelationid;

        public EventSourcingTestHelper()
        {
            this.Events = new List<IVersionedEvent>();
            this.repository =
                new RepositoryStub((eventSouced, correlationId) =>
                    {
                        if (this.expectedCorrelationid != null)
                        {
                            Assert.Equal(this.expectedCorrelationid, correlationId);
                        }

                        this.Events.AddRange(eventSouced.Events);
                    });
        }

        public List<IVersionedEvent> Events { get; private set; }

        public IEventSourcedRepository<T> Repository { get { return this.repository; } }

        public void Setup(ICommandHandler handler)
        {
            this.handler = handler;
        }

        public void Given(params IVersionedEvent[] history)
        {
            this.repository.History.AddRange(history);
        }

        public void When(ICommand command)
        {
            this.expectedCorrelationid = command.Id.ToString();
            ((dynamic)this.handler).Handle((dynamic)command);
            this.expectedCorrelationid = null;
        }

        public void When(IEvent @event)
        {
            ((dynamic)this.handler).Handle((dynamic)@event);
        }

        public bool ThenContains<TEvent>() where TEvent : IVersionedEvent
        {
            return this.Events.Any(x => x.GetType() == typeof(TEvent));
        }

        public TEvent ThenHasSingle<TEvent>() where TEvent : IVersionedEvent
        {
            Assert.Equal(1, this.Events.Count);
            var @event = this.Events.Single();
            Assert.IsAssignableFrom<TEvent>(@event);
            return (TEvent)@event;
        }

        public TEvent ThenHasOne<TEvent>() where TEvent : IVersionedEvent
        {
            Assert.Equal(1, this.Events.OfType<TEvent>().Count());
            var @event = this.Events.OfType<TEvent>().Single();
            return @event;
        }

        private class RepositoryStub : IEventSourcedRepository<T>
        {
            public readonly List<IVersionedEvent> History = new List<IVersionedEvent>();
            private readonly Action<T, string> onSave;
            private readonly Func<Guid, IEnumerable<IVersionedEvent>, T> entityFactory;

            internal RepositoryStub(Action<T, string> onSave)
            {
                this.onSave = onSave;
                var constructor = typeof(T).GetConstructor(new[] { typeof(Guid), typeof(IEnumerable<IVersionedEvent>) });
                if (constructor == null)
                {
                    throw new InvalidCastException(
                        "Type T must have a constructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
                }
                this.entityFactory = (id, events) => (T)constructor.Invoke(new object[] { id, events });
            }

            T IEventSourcedRepository<T>.Find(Guid id)
            {
                var all = this.History.Where(x => x.SourceId == id).ToList();
                if (all.Count > 0)
                {
                    return this.entityFactory.Invoke(id, all);
                }

                return default(T);
            }

            void IEventSourcedRepository<T>.Save(T eventSourced, string correlationId)
            {
                this.onSave(eventSourced, correlationId);
            }

            T IEventSourcedRepository<T>.Get(Guid id)
            {
                var entity = ((IEventSourcedRepository<T>)this).Find(id);
                if (Equals(entity, default(T)))
                {
                    throw new EntityNotFoundException(id, "Test");
                }

                return entity;
            }
        }
    }
}

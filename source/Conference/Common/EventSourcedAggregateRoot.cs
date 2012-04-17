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

namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public abstract class EventSourcedAggregateRoot : IEventSourcedAggregateRoot
    {
        private readonly Dictionary<Type, Action<IDomainEvent>> handlers = new Dictionary<Type, Action<IDomainEvent>>();
        private readonly List<IDomainEvent> pendingEvents = new List<IDomainEvent>();
        private int version = -1;

        public abstract Guid Id { get; }

        public int Version { get { return this.version; } }

        public IEnumerable<IDomainEvent> Events
        {
            get { return this.pendingEvents; }
        }

        /// <summary>
        /// Configures a handler for an event. 
        /// </summary>
        protected virtual void Handles<TEvent>(Action<TEvent> handler)
            where TEvent : IEvent
        {
            this.handlers.Add(typeof(TEvent), @event => handler((TEvent)@event));
        }

        protected void Rehydrate(IEnumerable<IDomainEvent> pastEvents)
        {
            foreach (var e in pastEvents)
            {
                this.handlers[e.GetType()].Invoke(e);
                this.version = e.Version;
            }
        }

        protected void Update(IDomainEvent e)
        {
            this.handlers[e.GetType()].Invoke(e);
            Debug.Assert(e.Version == this.version + 1);
            this.version = e.Version;
            this.pendingEvents.Add(e);
        }
    }
}

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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MemoryEventBus : IEventBus, IEventHandlerRegistry
    {
        private List<IEventHandler> handlers = new List<IEventHandler>();
        private List<IEvent> events = new List<IEvent>();

        public MemoryEventBus(params IEventHandler[] handlers)
        {
            this.handlers.AddRange(handlers);
        }

        public void Register(IEventHandler handler)
        {
            this.handlers.Add(handler);
        }

        public void Publish(IEvent @event)
        {
            this.events.Add(@event);

            Task.Factory.StartNew(() =>
            {
                var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());

                foreach (dynamic handler in this.handlers
                    .Where(x => handlerType.IsAssignableFrom(x.GetType())))
                {
                    handler.Handle((dynamic)@event);
                }
            });
        }

        public void Publish(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                this.Publish(@event);
            }
        }

        public IEnumerable<IEvent> Events { get { return this.events; } }
    }
}

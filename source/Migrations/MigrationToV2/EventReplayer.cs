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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;

    public class EventReplayer
    {
        private IEnumerable<IEventHandler> handlers;

        public EventReplayer(IEnumerable<IEventHandler> handlers)
        {
            this.handlers = handlers;
        }

        public void ReplayEvents(IEnumerable<IEvent> events)
        {
            foreach (var @event in events)
            {
                Trace.WriteLine(BuildEventDescription(@event));

                var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());

                foreach (dynamic handler in this.handlers
                    .Where(x => handlerType.IsAssignableFrom(x.GetType())))
                {
                    Trace.WriteLine("-- Handled by " + ((object)handler).GetType().FullName);
                    handler.Handle((dynamic)@event);
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
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

namespace Infrastructure.Azure.Instrumentation
{
    using System.Diagnostics;

    public class EventStoreBusPublisherInstrumentation : IEventStoreBusPublisherInstrumentation
    {
        public const string CurrentEventPublishersCounterName = "Event publishers";
        public const string TotalEventsPublishingRequestsCounterName = "Total events publishing requested";
        public const string EventPublishingRequestsPerSecondCounterName = "Event publishing requests/sec";
        public const string TotalEventsPublishedCounterName = "Total events published";
        public const string EventsPublishedPerSecondCounterName = "Events published/sec";

        private readonly bool instrumentationEnabled;

        private readonly PerformanceCounter currentEventPublishersCounter;
        private readonly PerformanceCounter totalEventsPublishingRequestedCounter;
        private readonly PerformanceCounter eventPublishingRequestsPerSecondCounter;
        private readonly PerformanceCounter eventsPublishedPerSecondCounter;
        private readonly PerformanceCounter totalEventsPublishedCounter;

        public EventStoreBusPublisherInstrumentation(bool instrumentationEnabled)
        {
            this.instrumentationEnabled = instrumentationEnabled;

            if (this.instrumentationEnabled)
            {
                this.currentEventPublishersCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, CurrentEventPublishersCounterName, false);
                this.totalEventsPublishingRequestedCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, TotalEventsPublishingRequestsCounterName, false);
                this.eventPublishingRequestsPerSecondCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, EventPublishingRequestsPerSecondCounterName, false);
                this.totalEventsPublishedCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, TotalEventsPublishedCounterName, false);
                this.eventsPublishedPerSecondCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, EventsPublishedPerSecondCounterName, false);
            }
        }

        public void EventsPublishingRequested(int eventCount)
        {
            if (this.instrumentationEnabled)
            {
                this.totalEventsPublishingRequestedCounter.IncrementBy(eventCount);
                this.eventPublishingRequestsPerSecondCounter.IncrementBy(eventCount);
            }
        }

        public void EventPublished()
        {
            if (this.instrumentationEnabled)
            {
                this.totalEventsPublishedCounter.Increment();
                this.eventsPublishedPerSecondCounter.Increment();
            }
        }

        public void EventPublisherStarted()
        {
            if (this.instrumentationEnabled)
            {
                this.currentEventPublishersCounter.Increment();
            }
        }

        public void EventPublisherFinished()
        {
            if (this.instrumentationEnabled)
            {
                this.currentEventPublishersCounter.Decrement();
            }
        }
    }
}

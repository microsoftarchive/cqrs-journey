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

namespace Infrastructure.Azure.Instrumentation
{
    using System;
    using System.Diagnostics;

    public class EventStoreBusPublisherInstrumentation : IEventStoreBusPublisherInstrumentation, IDisposable
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

        public EventStoreBusPublisherInstrumentation(string instanceName, bool instrumentationEnabled)
        {
            this.instrumentationEnabled = instrumentationEnabled;

            if (this.instrumentationEnabled)
            {
                this.currentEventPublishersCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, CurrentEventPublishersCounterName, instanceName, false);
                this.totalEventsPublishingRequestedCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, TotalEventsPublishingRequestsCounterName, instanceName, false);
                this.eventPublishingRequestsPerSecondCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, EventPublishingRequestsPerSecondCounterName, instanceName, false);
                this.totalEventsPublishedCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, TotalEventsPublishedCounterName, instanceName, false);
                this.eventsPublishedPerSecondCounter = new PerformanceCounter(Constants.EventPublishersPerformanceCountersCategory, EventsPublishedPerSecondCounterName, instanceName, false);

                this.currentEventPublishersCounter.RawValue = 0;
                this.totalEventsPublishingRequestedCounter.RawValue = 0;
                this.eventPublishingRequestsPerSecondCounter.RawValue = 0;
                this.totalEventsPublishedCounter.RawValue = 0;
                this.eventsPublishedPerSecondCounter.RawValue = 0;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.instrumentationEnabled)
                {
                    this.currentEventPublishersCounter.Dispose();
                    this.totalEventsPublishingRequestedCounter.Dispose();
                    this.eventPublishingRequestsPerSecondCounter.Dispose();
                    this.eventsPublishedPerSecondCounter.Dispose();
                    this.totalEventsPublishedCounter.Dispose();
                }
            }
        }
    }
}

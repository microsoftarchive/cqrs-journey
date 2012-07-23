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

    public class SubscriptionReceiverInstrumentation : ISubscriptionReceiverInstrumentation, IDisposable
    {
        public const string TotalMessagesCounterName = "Total messages received";
        public const string TotalMessagesSuccessfullyProcessedCounterName = "Total messages processed";
        public const string TotalMessagesUnsuccessfullyProcessedCounterName = "Total messages processed (fails)";
        public const string TotalMessagesCompletedCounterName = "Total messages completed";
        public const string TotalMessagesNotCompletedCounterName = "Total messages not completed";
        public const string MessagesReceivedPerSecondCounterName = "Messages received/sec";
        public const string AverageMessageProcessingTimeCounterName = "Avg. message processing time";
        public const string AverageMessageProcessingTimeBaseCounterName = "Avg. message processing time base";
        public const string CurrentMessagesInProcessCounterName = "Current messages";

        private readonly string instanceName;
        private readonly bool instrumentationEnabled;

        private readonly PerformanceCounter totalMessagesCounter;
        private readonly PerformanceCounter totalMessagesSuccessfullyProcessedCounter;
        private readonly PerformanceCounter totalMessagesUnsuccessfullyProcessedCounter;
        private readonly PerformanceCounter totalMessagesCompletedCounter;
        private readonly PerformanceCounter totalMessagesNotCompletedCounter;
        private readonly PerformanceCounter messagesReceivedPerSecondCounter;
        private readonly PerformanceCounter averageMessageProcessingTimeCounter;
        private readonly PerformanceCounter averageMessageProcessingTimeBaseCounter;
        private readonly PerformanceCounter currentMessagesInProcessCounter;

        public SubscriptionReceiverInstrumentation(string instanceName, bool instrumentationEnabled)
        {
            this.instanceName = instanceName;
            this.instrumentationEnabled = instrumentationEnabled;

            if (this.instrumentationEnabled)
            {
                this.totalMessagesCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, TotalMessagesCounterName, this.instanceName, false);
                this.totalMessagesSuccessfullyProcessedCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, TotalMessagesSuccessfullyProcessedCounterName, this.instanceName, false);
                this.totalMessagesUnsuccessfullyProcessedCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, TotalMessagesUnsuccessfullyProcessedCounterName, this.instanceName, false);
                this.totalMessagesCompletedCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, TotalMessagesCompletedCounterName, this.instanceName, false);
                this.totalMessagesNotCompletedCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, TotalMessagesNotCompletedCounterName, this.instanceName, false);
                this.messagesReceivedPerSecondCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, MessagesReceivedPerSecondCounterName, this.instanceName, false);
                this.averageMessageProcessingTimeCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, AverageMessageProcessingTimeCounterName, this.instanceName, false);
                this.averageMessageProcessingTimeBaseCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, AverageMessageProcessingTimeBaseCounterName, this.instanceName, false);
                this.currentMessagesInProcessCounter = new PerformanceCounter(Constants.ReceiversPerformanceCountersCategory, CurrentMessagesInProcessCounterName, this.instanceName, false);

                this.totalMessagesCounter.RawValue = 0;
                this.totalMessagesSuccessfullyProcessedCounter.RawValue = 0;
                this.totalMessagesUnsuccessfullyProcessedCounter.RawValue = 0;
                this.totalMessagesCompletedCounter.RawValue = 0;
                this.totalMessagesNotCompletedCounter.RawValue = 0;
                this.averageMessageProcessingTimeCounter.RawValue = 0;
                this.averageMessageProcessingTimeBaseCounter.RawValue = 0;
                this.currentMessagesInProcessCounter.RawValue = 0;
            }
        }

        protected string InstanceName
        {
            get { return this.instanceName; }
        }

        protected bool InstrumentationEnabled
        {
            get { return this.instrumentationEnabled; }
        }

        public void MessageReceived()
        {
            if (this.InstrumentationEnabled)
            {
                try
                {
                    this.totalMessagesCounter.Increment();
                    this.messagesReceivedPerSecondCounter.Increment();
                    this.currentMessagesInProcessCounter.Increment();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public void MessageProcessed(bool success, long elapsedMilliseconds)
        {
            if (this.InstrumentationEnabled)
            {
                try
                {
                    if (success)
                    {
                        this.totalMessagesSuccessfullyProcessedCounter.Increment();
                    }
                    else
                    {
                        this.totalMessagesUnsuccessfullyProcessedCounter.Increment();
                    }

                    this.averageMessageProcessingTimeCounter.IncrementBy(elapsedMilliseconds / 100);
                    this.averageMessageProcessingTimeBaseCounter.Increment();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public void MessageCompleted(bool success)
        {
            if (this.InstrumentationEnabled)
            {
                try
                {
                    if (success)
                    {
                        this.totalMessagesCompletedCounter.Increment();
                    }
                    else
                    {
                        this.totalMessagesNotCompletedCounter.Increment();
                    }
                    this.currentMessagesInProcessCounter.Decrement();
                }
                catch (ObjectDisposedException)
                {
                }
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
                    this.totalMessagesCounter.Dispose();
                    this.totalMessagesSuccessfullyProcessedCounter.Dispose();
                    this.totalMessagesUnsuccessfullyProcessedCounter.Dispose();
                    this.totalMessagesCompletedCounter.Dispose();
                    this.totalMessagesNotCompletedCounter.Dispose();
                    this.messagesReceivedPerSecondCounter.Dispose();
                    this.averageMessageProcessingTimeCounter.Dispose();
                    this.averageMessageProcessingTimeBaseCounter.Dispose();
                    this.currentMessagesInProcessCounter.Dispose();
                }
            }
        }
    }
}

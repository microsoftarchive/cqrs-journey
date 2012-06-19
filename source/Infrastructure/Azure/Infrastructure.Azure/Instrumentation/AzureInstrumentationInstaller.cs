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
    using System.ComponentModel;
    using System.Diagnostics;

    [RunInstaller(true)]
    public partial class AzureInstrumentationInstaller : System.Configuration.Install.Installer
    {
        public AzureInstrumentationInstaller()
        {
            InitializeComponent();

            var installer = new PerformanceCounterInstaller { CategoryName = Constants.PerformanceCountersCategory, CategoryType = PerformanceCounterCategoryType.MultiInstance };

            installer.Counters.Add(new CounterCreationData(SessionSubscriptionReceiverInstrumentation.TotalSessionsCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));

            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.TotalMessagesCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.TotalMessagesSuccessfullyProcessedCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.TotalMessagesUnsuccessfullyProcessedCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.TotalMessagesCompletedCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));
            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.TotalMessagesNotCompletedCounterName, string.Empty, PerformanceCounterType.NumberOfItems32));

            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.AverageMessageProcessingTimeCounterName, string.Empty, PerformanceCounterType.RawFraction));
            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.AverageMessageProcessingTimeBaseCounterName, string.Empty, PerformanceCounterType.RawBase));

            installer.Counters.Add(new CounterCreationData(SubscriptionReceiverInstrumentation.MessagesReceivedPerSecondCounterName, string.Empty, PerformanceCounterType.RateOfCountsPerSecond32));

            this.Installers.Add(installer);
        }
    }
}

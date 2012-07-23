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

namespace Conference.Web.Public
{
    using System;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public override bool OnStart()
        {
            var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

            var cloudStorageAccount =
                CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

            TimeSpan transferPeriod;
            if (!TimeSpan.TryParse(RoleEnvironment.GetConfigurationSettingValue("Diagnostics.ScheduledTransferPeriod"), out transferPeriod))
            {
                transferPeriod = TimeSpan.FromMinutes(1);
            }

            TimeSpan sampleRate;
            if (!TimeSpan.TryParse(RoleEnvironment.GetConfigurationSettingValue("Diagnostics.PerformanceCounterSampleRate"), out sampleRate))
            {
                sampleRate = TimeSpan.FromSeconds(30);
            }

            LogLevel logLevel;
            if (!Enum.TryParse<LogLevel>(RoleEnvironment.GetConfigurationSettingValue("Diagnostics.LogLevelFilter"), out logLevel))
            {
                logLevel = LogLevel.Verbose;
            }

            // Setup performance counters
            config.PerformanceCounters.DataSources.Add(
                new PerformanceCounterConfiguration
                {
                    CounterSpecifier = @"\Processor(_Total)\% Processor Time",
                    SampleRate = sampleRate
                });
            config.PerformanceCounters.ScheduledTransferPeriod = transferPeriod;
#if !LOCAL
            foreach (var counterName in
                new[] 
                { 
                    Infrastructure.Azure.Instrumentation.EventStoreBusPublisherInstrumentation.CurrentEventPublishersCounterName,
                    Infrastructure.Azure.Instrumentation.EventStoreBusPublisherInstrumentation.EventPublishingRequestsPerSecondCounterName,
                    Infrastructure.Azure.Instrumentation.EventStoreBusPublisherInstrumentation.EventsPublishedPerSecondCounterName,
                    Infrastructure.Azure.Instrumentation.EventStoreBusPublisherInstrumentation.TotalEventsPublishedCounterName,
                    Infrastructure.Azure.Instrumentation.EventStoreBusPublisherInstrumentation.TotalEventsPublishingRequestsCounterName,
                })
            {
                config.PerformanceCounters.DataSources.Add(
                    new PerformanceCounterConfiguration
                    {
                        CounterSpecifier = @"\" + Infrastructure.Azure.Instrumentation.Constants.EventPublishersPerformanceCountersCategory + @"(*)\" + counterName,
                        SampleRate = sampleRate
                    });
            }
#endif

            // Setup logs
            config.Logs.ScheduledTransferPeriod = transferPeriod;
            config.Logs.ScheduledTransferLogLevelFilter = logLevel;

            DiagnosticMonitor.Start(cloudStorageAccount, config);

            return base.OnStart();
        }
    }
}

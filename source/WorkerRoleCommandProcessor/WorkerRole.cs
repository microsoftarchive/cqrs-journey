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

namespace WorkerRoleCommandProcessor
{
    using System;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Conference.Common;
    using Conference.Common.Entity;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WorkerRole : RoleEntryPoint
    {
        private bool running;

        public override void Run()
        {
            TaskScheduler.UnobservedTaskException += this.OnUnobservedTaskException;
            this.running = true;

            MaintenanceMode.RefreshIsInMaintainanceMode();
            if (!MaintenanceMode.IsInMaintainanceMode)
            {
                Trace.WriteLine("Starting the command processor", "Information");
                using (var processor = new ConferenceProcessor())
                {
                    processor.Start();

                    while (this.running)
                    {
                        Thread.Sleep(10000);
                    }

                    processor.Stop();
                }
            }
            else
            {
                Trace.TraceWarning("Starting the command processor in mantainance mode.");
                while (this.running)
                {
                    Thread.Sleep(10000);
                }
            }

            TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Trace.TraceError("Unobserved task exception: \r\n{0}", e.Exception);
        }

        public override bool OnStart()
        {
            RoleEnvironment.Changing += (sender, e) =>
            {
                if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                {
                    // Set e.Cancel to true to restart this role instance
                    e.Cancel = true;
                }
            };

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

            // Setup logs
            config.Logs.ScheduledTransferPeriod = transferPeriod;
            config.Logs.ScheduledTransferLogLevelFilter = logLevel;

            DiagnosticMonitor.Start(cloudStorageAccount, config);

            Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());
            Trace.AutoFlush = true;

            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            // Setup V3 migrations.
            // In future revisions, this line will change to invoke a V4 migration (possibly)
            // and the initialization of the V3 migration won't be needed anymore, as the 
            // production database will already have been migrated to V3.
            MigrationToV3.Migration.Initialize();

            return base.OnStart();
        }

        public override void OnStop()
        {
            this.running = false;
            base.OnStop();
        }
    }
}

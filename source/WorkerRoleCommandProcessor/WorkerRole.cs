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
    using Conference.Common.Entity;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WorkerRole : RoleEntryPoint
    {
        private bool running;

        public override void Run()
        {
            Trace.WriteLine("Starting the command processor", "Information");

            this.running = true;

#if !LOCAL
            using (var processor = new ConferenceCommandProcessor())
            {
                processor.Start();

                while (this.running)
                {
                    Thread.Sleep(10000);
                    Trace.WriteLine("Command processor working", "Information");
                }

                processor.Stop();
            }
#else
            while (this.running)
            {
                Thread.Sleep(10000);
                Trace.WriteLine("Command processor idling", "Information");
            }
#endif
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

            // Get the logs as fast as possible
            config.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);
            config.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;

            DiagnosticMonitor.Start(cloudStorageAccount, config);

            Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());
            Trace.AutoFlush = true;

            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            return base.OnStart();
        }

        public override void OnStop()
        {
            this.running = false;
            base.OnStop();
        }
    }
}

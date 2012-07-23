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

namespace MigrationToV3.InHouseProcessor
{
    using System;
    using System.Diagnostics;
    using System.Threading;
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

            while (this.running)
            {
                Trace.WriteLine("Running processor", "Information");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        public override bool OnStart()
        {
            var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

            var cloudStorageAccount =
                CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

            var transferPeriod = TimeSpan.FromMinutes(5);
            var logLevel = LogLevel.Verbose;

            config.Logs.ScheduledTransferPeriod = transferPeriod;
            config.Logs.ScheduledTransferLogLevelFilter = logLevel;

            DiagnosticMonitor.Start(cloudStorageAccount, config);

            Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());
            Trace.AutoFlush = true;

            return base.OnStart();
        }

        public override void OnStop()
        {
            this.running = false;
            base.OnStop();
        }
    }
}

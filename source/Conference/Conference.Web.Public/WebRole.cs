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

namespace Conference.Web.Public
{
    using System;
    using System.Linq;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WebRole : RoleEntryPoint
    {
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

            return base.OnStart();
        }
    }
}

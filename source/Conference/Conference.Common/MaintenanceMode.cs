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

namespace Conference.Common
{
    using System;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class MaintenanceMode
    {
        public const string MaintenanceModeSettingName = "MaintenanceMode";

        public static bool IsInMaintainanceMode { get; internal set; }

        public static void RefreshIsInMaintainanceMode()
        {
            if (RoleEnvironment.IsAvailable)
            {
                try
                {
                    var settingValue = RoleEnvironment.GetConfigurationSettingValue(MaintenanceModeSettingName);
                    IsInMaintainanceMode = (!string.IsNullOrEmpty(settingValue) &&
                                            string.Equals(settingValue, "true", StringComparison.OrdinalIgnoreCase));
                }
                catch (RoleEnvironmentException)
                {
                    // setting does not exist, assume is not in maintenance mode.
                    IsInMaintainanceMode = false;
                }
            }
            else
            {
                IsInMaintainanceMode = false;
            }
        }
    }
}

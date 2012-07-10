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

namespace Conference.Common.Entity
{
    using System.Configuration;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure;
    using Microsoft.WindowsAzure;

    public class ServiceConfigurationSettingConnectionFactory : IDbConnectionFactory
    {
        private IDbConnectionFactory parent;

        public ServiceConfigurationSettingConnectionFactory(IDbConnectionFactory parent)
        {
            this.parent = parent;
        }

        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            string connectionString = null;
            if (!IsConnectionString(nameOrConnectionString))
            {
                var connectionStringName = "DbContext." + nameOrConnectionString;
                var settingValue = CloudConfigurationManager.GetSetting(connectionStringName);
                if (!string.IsNullOrEmpty(settingValue))
                {
                    connectionString = settingValue;
                }

                if (connectionString == null)
                {
                    try
                    {
                        var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
                        if (connectionStringSettings != null)
                        {
                            connectionString = connectionStringSettings.ConnectionString;
                        }
                    }
                    catch (ConfigurationErrorsException)
                    {
                    }
                }
            }

            if (connectionString == null)
            {
                connectionString = nameOrConnectionString;
            }

            return this.parent.CreateConnection(connectionString);
        }

        private static bool IsConnectionString(string connectionStringCandidate)
        {
            return (connectionStringCandidate.IndexOf('=') >= 0);
        }
    }
}

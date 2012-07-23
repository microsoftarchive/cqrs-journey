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

namespace Conference.Common.Entity
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Microsoft.WindowsAzure;

    public class ServiceConfigurationSettingConnectionFactory : IDbConnectionFactory
    {
        private readonly object lockObject = new object();
        private readonly IDbConnectionFactory parent;
        private Dictionary<string, string> cachedConnectionStringsMap = new Dictionary<string, string>();

        public ServiceConfigurationSettingConnectionFactory(IDbConnectionFactory parent)
        {
            this.parent = parent;
        }

        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            string connectionString = null;
            if (!IsConnectionString(nameOrConnectionString))
            {
                if (!this.cachedConnectionStringsMap.TryGetValue(nameOrConnectionString, out connectionString))
                {
                    lock (this.lockObject)
                    {
                        if (!this.cachedConnectionStringsMap.TryGetValue(nameOrConnectionString, out connectionString))
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

                            var immutableDictionary = this.cachedConnectionStringsMap
                                .Concat(new[] { new KeyValuePair<string, string>(nameOrConnectionString, connectionString) })
                                .ToDictionary(x => x.Key, x => x.Value);

                            this.cachedConnectionStringsMap = immutableDictionary;
                        }
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

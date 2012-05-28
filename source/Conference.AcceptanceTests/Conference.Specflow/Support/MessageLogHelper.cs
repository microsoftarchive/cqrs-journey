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

using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure.MessageLog;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
#if LOCAL
using Infrastructure.Sql.MessageLog;
using Conference.Common.Entity;
using Infrastructure;
using System.Data.Entity;
#else
using Infrastructure.Azure.MessageLog;
using Microsoft.WindowsAzure;
using Infrastructure.Azure;
#endif

namespace Conference.Specflow.Support
{
    public static class MessageLogHelper
    {
        private static readonly IEventLogReader eventLog;

        static MessageLogHelper()
        {
            var serializer = new JsonTextSerializer();
#if LOCAL
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);
            Database.SetInitializer<MessageLogDbContext>(null);
            eventLog = new SqlMessageLog("MessageLog", serializer, new StandardMetadataProvider());
#else
            var settings = InfrastructureSettings.Read("Settings.xml").MessageLog;
            var account = CloudStorageAccount.Parse(settings.ConnectionString);
            eventLog = new AzureEventLogReader(account, settings.TableName, serializer);
#endif
        }

        public static IEnumerable<T> GetEvents<T>(Guid sourceId) where T : IEvent
        {
            return GetEvents<T>(new [] { sourceId.ToString() });
        }

        public static IEnumerable<T> GetEvents<T>(ICollection<string> sourceIds) where T : IEvent
        {
            var criteria = new QueryCriteria { FullNames = {typeof (T).FullName}};
            criteria.SourceIds.AddRange(sourceIds);
            return eventLog.Query(criteria).OfType<T>();
        }
    }
}

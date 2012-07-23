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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conference.Specflow.Support.MessageLog;
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
        private static readonly ICommandLogReader commandLog;

        static MessageLogHelper()
        {
            var serializer = new JsonTextSerializer();
#if LOCAL
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);
            Database.SetInitializer<MessageLogDbContext>(null);
            eventLog = new SqlMessageLog("MessageLog", serializer, new StandardMetadataProvider());
            commandLog = new SqlCommandMessageLog("MessageLog", serializer, new StandardMetadataProvider());
#else
            var settings = InfrastructureSettings.Read("Settings.xml").MessageLog;
            var account = CloudStorageAccount.Parse(settings.ConnectionString);
            eventLog = new AzureEventLogReader(account, settings.TableName, serializer);
            commandLog = new AzureCommandLogReader(account, settings.TableName, serializer);
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

        public static IEnumerable<T> GetCommands<T>() where T : ICommand
        {
            var criteria = new QueryCriteria { FullNames = { typeof(T).FullName } };
            return commandLog.Query(criteria).OfType<T>();
        }

        public static IEnumerable<T> GetCommands<T>(Guid sourceId) where T : ICommand
        {
            return GetCommands<T>(new[] { sourceId.ToString() });
        }

        public static IEnumerable<T> GetCommands<T>(ICollection<string> sourceIds) where T : ICommand
        {
            var criteria = new QueryCriteria { FullNames = { typeof(T).FullName } };
            criteria.SourceIds.AddRange(sourceIds);
            return commandLog.Query(criteria).OfType<T>();
        }

        public static bool CollectEvents<T>(Guid sourceId, int count) where T : IEvent
        {
            return CollectEvents<T>(new[] { sourceId.ToString() }, count);
        }

        public static bool CollectEvents<T>(ICollection<string> sourceIds, int count) where T : IEvent
        {
            var timeout = DateTime.Now.Add(Constants.UI.WaitTimeout);
            int collected;
            do
            {
                collected = GetEvents<T>(sourceIds).Count();
                Thread.Sleep(100);
            } while (collected != count && DateTime.Now < timeout);

            return collected == count;
        }

        public static bool CollectEvents<T>(Func<T, bool> predicate) where T : IEvent
        {
            var timeout = DateTime.Now.Add(Constants.UI.WaitTimeout);
            var criteria = new QueryCriteria { FullNames = { typeof(T).FullName } };
            bool found;
            while (!(found = eventLog.Query(criteria).OfType<T>().ToList().Any(predicate)) && DateTime.Now < timeout)
            {
                Thread.Sleep(100);
            }
            return found;
        }

        public static bool CollectCommands<T>(Guid sourceId, int count) where T : ICommand
        {
            return CollectCommands<T>(new[] { sourceId.ToString() }, count);
        }

        public static bool CollectCommands<T>(ICollection<string> sourceIds, int count) where T : ICommand
        {
            var timeout = DateTime.Now.Add(Constants.UI.WaitTimeout);
            int collected;
            do
            {
                collected = GetCommands<T>(sourceIds).Count();
                Thread.Sleep(100);
            } while (collected != count && DateTime.Now < timeout);

            return collected == count;
        }

        public static bool CollectCommands<T>(Func<T, bool> predicate) where T : ICommand
        {
            var timeout = DateTime.Now.Add(Constants.UI.WaitTimeout);
            var criteria = new QueryCriteria { FullNames = { typeof(T).FullName } };
            bool found;
            while (!(found = commandLog.Query(criteria).OfType<T>().Any(predicate)) && DateTime.Now < timeout)
            {
                Thread.Sleep(100);
            }
            return found;
        }
    }
}

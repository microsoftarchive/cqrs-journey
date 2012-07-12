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
using Infrastructure.Azure.Instrumentation;
using Infrastructure.EventSourcing;
using Infrastructure.Serialization;
#if LOCAL
using System.Data.Entity;
using Infrastructure.Sql.EventSourcing;
#else
using Infrastructure;
using Infrastructure.Azure;
using Infrastructure.Azure.EventSourcing;
using Microsoft.WindowsAzure;
using System.Runtime.Caching;
#endif

namespace Conference.Specflow.Support
{
    public static class EventSourceHelper
    {
#if LOCAL
        static EventSourceHelper()
        {
            Database.SetInitializer<EventStoreDbContext>(null);
        }
#endif
        public static IEventSourcedRepository<T> GetRepository<T>() where T : class, IEventSourced
        {
            var serializer = new JsonTextSerializer();
#if LOCAL
            Func<EventStoreDbContext> ctxFactory = () => new EventStoreDbContext("EventStore");
            return new SqlEventSourcedRepository<T>(ConferenceHelper.BuildEventBus(), serializer, ctxFactory);
#else
            var settings = InfrastructureSettings.Read("Settings.xml");
            var eventSourcingAccount = CloudStorageAccount.Parse(settings.EventSourcing.ConnectionString);
            var eventStore = new EventStore(eventSourcingAccount, settings.EventSourcing.TableName);
            var publisher = new EventStoreBusPublisher(ConferenceHelper.GetTopicSender("events"), eventStore, new EventStoreBusPublisherInstrumentation("worker", false));
            var metadata = new StandardMetadataProvider();
            return new AzureEventSourcedRepository<T>(eventStore, publisher, serializer, metadata, new MemoryCache("RepositoryCache"));
#endif
        }
    }
}

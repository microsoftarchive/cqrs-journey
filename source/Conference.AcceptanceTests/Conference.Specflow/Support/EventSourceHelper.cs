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
using Infrastructure.Azure.Instrumentation;
using Infrastructure.EventSourcing;
using Infrastructure.Serialization;
using Registration;
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
        public static IEventSourcedRepository<SeatsAvailability> GetSeatsAvailabilityRepository()
        {
            var serializer = new JsonTextSerializer();
#if LOCAL
            Func<EventStoreDbContext> ctxFactory = () => new EventStoreDbContext("EventStore");
            return new SqlEventSourcedRepository<SeatsAvailability>(ConferenceHelper.BuildEventBus(), serializer, ctxFactory);
#else
            var settings = InfrastructureSettings.Read("Settings.xml");
            var eventSourcingAccount = CloudStorageAccount.Parse(settings.EventSourcing.ConnectionString);
            var eventStore = new EventStore(eventSourcingAccount, settings.EventSourcing.SeatsAvailabilityTableName);
            var publisher = new EventStoreBusPublisher(ConferenceHelper.GetTopicSender("eventsAvailability"), eventStore, new EventStoreBusPublisherInstrumentation("worker", false));
            var metadata = new StandardMetadataProvider();
            return new AzureEventSourcedRepository<SeatsAvailability>(eventStore, publisher, serializer, metadata, new MemoryCache("RepositoryCache"));
#endif
        }
    }
}

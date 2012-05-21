using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infrastructure.Azure.EventSourcing;
using Infrastructure.Azure.Messaging;
using Infrastructure.EventSourcing;
using Infrastructure.Serialization;
using Microsoft.WindowsAzure;

namespace Conference.Specflow.Support
{
    public static class EventSourceHelper
    {
        public static IEventSourcedRepository<T> GetRepository<T>() where T : class, IEventSourced
        {
            var eventSourcingSettings = InfrastructureSettings.ReadEventSourcing("Settings.xml");
            var eventSourcingAccount = CloudStorageAccount.Parse(eventSourcingSettings.ConnectionString);
            var eventStore = new EventStore(eventSourcingAccount, eventSourcingSettings.TableName);
            var publisher = new EventStoreBusPublisher(ConferenceHelper.GetTopicSender("events"), eventStore);

            return new AzureEventSourcedRepository<T>(eventStore, publisher, new JsonTextSerializer());
        }
    }
}

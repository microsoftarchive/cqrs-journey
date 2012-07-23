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

namespace MigrationToV2
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Services.Client;
    using System.Threading;
    using AutoMapper;
    using Conference;
    using Infrastructure;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.EventSourcing;
    using Infrastructure.MessageLog;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.AzureStorage;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.WindowsAzure.StorageClient;
    using Registration.Handlers;
    using Registration.ReadModel.Implementation;

    public class Migrator
    {
        public Migrator()
        {
            Mapper.CreateMap<EventTableServiceEntity, EventTableServiceEntity>();
            Mapper.CreateMap<EventTableServiceEntity, MessageLogEntity>();
        }

        private readonly RetryPolicy<StorageTransientErrorDetectionStrategy> retryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));

        public void GeneratePastEventLogMessagesForConferenceManagement(
            CloudTableClient messageLogClient, string messageLogName,
            string conferenceManagementConnectionString,
            IMetadataProvider metadataProvider, ITextSerializer serializer)
        {
            retryPolicy.ExecuteAction(() => messageLogClient.CreateTableIfNotExist(messageLogName));

            // set the creation date to just before releasing V1 (previous month).
            var eventCreationDate = new DateTime(2012, 04, 01, 0, 0, 0, DateTimeKind.Utc);

            var generatedEvents = this.GenerateMissedConferenceManagementIntegrationEvents(conferenceManagementConnectionString);
            foreach (var evt in generatedEvents)
            {
                // generate events in ascending order. If there is a conflict when saving (currently silently swallowed by AzureEventLogWriter), 
                // then the migration process is being run for the second time, which is wrong.
                // TODO: what happens if the process crashes middleway.
                eventCreationDate = eventCreationDate.AddSeconds(1);
                var metadata = metadataProvider.GetMetadata(evt);
                var entry = new MessageLogEntity
                {
                    PartitionKey = eventCreationDate.ToString("yyyMM"),
                    // could have a prefix instead of suffix to be able to search
                    RowKey = eventCreationDate.Ticks.ToString("D20") + "_Generated",
                    CreationDate = eventCreationDate.ToString("o"),
                    MessageId = null,
                    CorrelationId = null,
                    SourceType = null,
                    SourceId = evt.SourceId.ToString(),
                    AssemblyName = metadata[StandardMetadata.AssemblyName],
                    FullName = metadata[StandardMetadata.FullName],
                    Namespace = metadata[StandardMetadata.Namespace],
                    TypeName = metadata[StandardMetadata.TypeName],
                    Kind = StandardMetadata.EventKind,
                    Payload = serializer.Serialize(evt),
                };

                var context = messageLogClient.GetDataServiceContext();
                context.AddObject(messageLogName, entry);
                retryPolicy.ExecuteAction(() => context.SaveChanges());
            }
        }

        public void MigrateEventSourcedAndGeneratePastEventLogs(
            CloudTableClient messageLogClient, string messageLogName,
            CloudTableClient originalEventStoreClient, string originalEventStoreName,
            CloudTableClient newEventStoreClient, string newEventStoreName,
            IMetadataProvider metadataProvider, ITextSerializer serializer)
        {
            retryPolicy.ExecuteAction(() => newEventStoreClient.CreateTableIfNotExist(newEventStoreName));

            var currentEventStoreContext = newEventStoreClient.GetDataServiceContext();
            string currentEventStorePartitionKey = null;
            int currentEventStoreCount = 0;

            var currentMessageLogContext = messageLogClient.GetDataServiceContext();
            string currentMessageLogPartitionKey = null;
            int currentMessageLogCount = 0;

            foreach (var esEntry in this.GetAllEventSourcingEntries(originalEventStoreClient, originalEventStoreName))
            {
                // Copies the original values from the stored entry
                var migratedEntry = Mapper.Map<EventTableServiceEntity>(esEntry);

                // get the metadata, as it was not stored in the event store
                var metadata = metadataProvider.GetMetadata(serializer.Deserialize<IVersionedEvent>(esEntry.Payload));
                migratedEntry.AssemblyName = metadata[StandardMetadata.AssemblyName];
                migratedEntry.FullName = metadata[StandardMetadata.FullName];
                migratedEntry.Namespace = metadata[StandardMetadata.Namespace];
                migratedEntry.TypeName = metadata[StandardMetadata.TypeName];
                migratedEntry.CreationDate = esEntry.Timestamp.ToString("o");

                if (currentEventStorePartitionKey == null)
                {
                    currentEventStorePartitionKey = migratedEntry.PartitionKey;
                    ++currentEventStoreCount;
                }
                else if (currentEventStorePartitionKey != migratedEntry.PartitionKey || ++currentEventStoreCount == 100)
                {
                    retryPolicy.ExecuteAction(() => currentEventStoreContext.SaveChanges(SaveChangesOptions.Batch));
                    currentEventStoreContext = newEventStoreClient.GetDataServiceContext();
                    currentEventStorePartitionKey = migratedEntry.PartitionKey;
                    currentEventStoreCount = 0;
                }

                currentEventStoreContext.AddObject(newEventStoreName, migratedEntry);

                const string RowKeyVersionLowerLimit = "0000000000";
                const string RowKeyVersionUpperLimit = "9999999999";

                if (migratedEntry.RowKey.CompareTo(RowKeyVersionLowerLimit) >= 0 &&
                    migratedEntry.RowKey.CompareTo(RowKeyVersionUpperLimit) <= 0)
                {
                    var messageId = migratedEntry.PartitionKey + "_" + migratedEntry.RowKey; //This is the message ID used in the past (deterministic).
                    var logEntry = Mapper.Map<MessageLogEntity>(migratedEntry);
                    logEntry.PartitionKey = esEntry.Timestamp.ToString("yyyMM");
                    logEntry.RowKey = esEntry.Timestamp.Ticks.ToString("D20") + "_" + messageId;
                    logEntry.MessageId = messageId;
                    logEntry.CorrelationId = null;
                    logEntry.Kind = StandardMetadata.EventKind;

                    if (currentMessageLogPartitionKey == null)
                    {
                        currentMessageLogPartitionKey = logEntry.PartitionKey;
                        ++currentMessageLogCount;
                    }
                    else if (currentMessageLogPartitionKey != logEntry.PartitionKey || ++currentMessageLogCount == 100)
                    {
                        retryPolicy.ExecuteAction(() => currentMessageLogContext.SaveChanges(SaveChangesOptions.Batch));
                        currentMessageLogContext = messageLogClient.GetDataServiceContext();
                        currentMessageLogPartitionKey = logEntry.PartitionKey;
                        currentMessageLogCount = 0;
                    }

                    currentMessageLogContext.AddObject(messageLogName, logEntry);
                }
            }

            // save any remaining entries
            retryPolicy.ExecuteAction(() => currentEventStoreContext.SaveChanges(SaveChangesOptions.Batch));
            retryPolicy.ExecuteAction(() => currentMessageLogContext.SaveChanges(SaveChangesOptions.Batch));
        }

        // Very similar to ConferenceService.cs
        private IEnumerable<IEvent> GenerateMissedConferenceManagementIntegrationEvents(string nameOrConnectionString)
        {
            // Note: instead of returning a list, could yield results if data set is very big. Seems unnecessary.
            var events = new List<IEvent>();
            using (var context = new ConferenceContext(nameOrConnectionString))
            {
                foreach (var conference in context.Conferences.Include(x => x.Seats))
                {
                    // Use automapper? I'd prefer this to be explicit just in case we don't make mistakes with versions.
                    events.Add(new ConferenceCreated
                    {
                        SourceId = conference.Id,
                        Owner = new Owner
                        {
                            Name = conference.OwnerName,
                            Email = conference.OwnerEmail,
                        },
                        Name = conference.Name,
                        Description = conference.Description,
                        Location = conference.Location,
                        Slug = conference.Slug,
                        Tagline = conference.Tagline,
                        TwitterSearch = conference.TwitterSearch,
                        StartDate = conference.StartDate,
                        EndDate = conference.EndDate,
                    });

                    foreach (var seat in conference.Seats)
                    {
                        events.Add(new SeatCreated
                        {
                            ConferenceId = conference.Id,
                            SourceId = seat.Id,
                            Name = seat.Name,
                            Description = seat.Description,
                            Price = seat.Price,
                            Quantity = seat.Quantity
                        });
                    }

                    if (conference.WasEverPublished)
                    {
                        events.Add(new ConferencePublished { SourceId = conference.Id });
                        if (!conference.IsPublished)
                        {
                            events.Add(new ConferenceUnpublished { SourceId = conference.Id });
                        }
                    }
                }
            }

            return events;
        }

        // Very similar to EventStore.cs
        private IEnumerable<EventTableServiceEntity> GetAllEventSourcingEntries(CloudTableClient tableClient, string tableName)
        {
            var context = tableClient.GetDataServiceContext();
            var query = context
                .CreateQuery<EventTableServiceEntity>(tableName)
                .AsTableServiceQuery();

            var result = new BlockingCollection<EventTableServiceEntity>();
            var tokenSource = new CancellationTokenSource();

            this.retryPolicy.ExecuteAction(
                ac => query.BeginExecuteSegmented(ac, null),
                ar => query.EndExecuteSegmented(ar),
                rs =>
                {
                    foreach (var key in rs.Results)
                    {
                        result.Add(key);
                    }

                    while (rs.HasMoreResults)
                    {
                        try
                        {
                            rs = this.retryPolicy.ExecuteAction(() => rs.GetNext());
                            foreach (var key in rs.Results)
                            {
                                result.Add(key);
                            }
                        }
                        catch
                        {
                            // Cancel is to force an exception being thrown in the consuming enumeration thread
                            // TODO: is there a better way to get the correct exception message instead of an OperationCancelledException in the consuming thread?
                            tokenSource.Cancel();
                            throw;
                        }
                    }
                    result.CompleteAdding();
                },
                ex =>
                {
                    tokenSource.Cancel();
                    throw ex;
                });

            return result.GetConsumingEnumerable(tokenSource.Token);
        }

        public void RegenerateViewModels(AzureEventLogReader logReader, string dbConnectionString)
        {
            var commandBus = new NullCommandBus();

            Database.SetInitializer<ConferenceRegistrationDbContext>(null);

            var handlers = new List<IEventHandler>();
            handlers.Add(new ConferenceViewModelGenerator(() => new ConferenceRegistrationDbContext(dbConnectionString), commandBus));
            handlers.Add(new PricedOrderViewModelUpdater(() => new ConferenceRegistrationDbContext(dbConnectionString)));

            using (var context = new ConferenceRegistrationMigrationDbContext(dbConnectionString))
            {
                context.UpdateTables();
            }

            try
            {
                var dispatcher = new EventDispatcher(handlers);
                var events = logReader.Query(new QueryCriteria { });

                dispatcher.DispatchMessages(events);
            }
            catch
            {
                using (var context = new ConferenceRegistrationMigrationDbContext(dbConnectionString))
                {
                    context.RollbackTablesMigration();
                }

                throw;
            }
        }
    }
}

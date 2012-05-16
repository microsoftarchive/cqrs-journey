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

namespace MigrationToV2
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Conference;
    using Infrastructure;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using System.Data.Entity;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.AzureStorage;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.WindowsAzure.StorageClient;

    public class Migrator
    {
        private readonly RetryPolicy<StorageTransientErrorDetectionStrategy> retryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));

        public void GeneratePastEventLogMessagesForConferenceManagement(AzureMessageLogWriter writer, string conferenceManagementConnectionString, IMetadataProvider metadataProvider, ITextSerializer serializer)
        {
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
                    // Timestamp cannot be set, and according to docs, should not be used for business logic.
                    // TODO: should we add and extra column storing event creation date? 
                    // we should do the same for EventTableServiceEntity and migrate schema (we probably need a migration of those anyway, to add new metadata)
                    // Timestamp = eventCreationDate, 
                    MessageId = null,
                    CorrelationId = null,
                    SourceId = evt.SourceId.ToString(),
                    AssemblyName = metadata[StandardMetadata.AssemblyName],
                    FullName = metadata[StandardMetadata.FullName],
                    Namespace = metadata[StandardMetadata.Namespace],
                    TypeName = metadata[StandardMetadata.TypeName],
                    Kind = StandardMetadata.EventKind,
                    Payload = serializer.Serialize(evt),
                };
                writer.Save(entry);
            }
        }

        public void GeneratePastEventLogMessagesForEventSourced(AzureMessageLogWriter writer, CloudTableClient eventSourcingTableClient, string eventSourcingTableName, IMetadataProvider metadataProvider, ITextSerializer serializer)
        {
            foreach (var esEntry in this.GetAllEventSourcingEntries(eventSourcingTableClient, eventSourcingTableName))
            {
                // get the metadata, as it was not stored in the event store.
                // TODO: should we update the event store with this metadata?
                var metadata = metadataProvider.GetMetadata(serializer.Deserialize<IVersionedEvent>(esEntry.Payload));
                var messageId = esEntry.PartitionKey + "_" + esEntry.RowKey; //This is the message ID used in the past (deterministic).
                var entry = new MessageLogEntity
                {
                    PartitionKey = esEntry.Timestamp.ToString("yyyMM"),
                    RowKey = esEntry.Timestamp.Ticks.ToString("D20") + "_" + messageId,
                    // Timestamp cannot be set, and according to docs, should not be used for business logic.
                    // TODO: should we add and extra column storing event creation date? 
                    // we should do the same for EventTableServiceEntity and migrate schema (we probably need a migration of those anyway, to add new metadata)
                    // Timestamp = esEntry.Timestamp, 
                    MessageId = null,
                    CorrelationId = null,
                    SourceId = esEntry.SourceId,
                    AssemblyName = metadata[StandardMetadata.AssemblyName],
                    FullName = metadata[StandardMetadata.FullName],
                    Namespace = metadata[StandardMetadata.Namespace],
                    TypeName = metadata[StandardMetadata.TypeName],
                    Kind = StandardMetadata.EventKind,
                    Payload = esEntry.Payload,
                };
                writer.Save(entry);
            }
        }

        // Very similar to ConferenceService.cs
        internal IEnumerable<IEvent> GenerateMissedConferenceManagementIntegrationEvents(string nameOrConnectionString)
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
        internal IEnumerable<EventTableServiceEntity> GetAllEventSourcingEntries(CloudTableClient tableClient, string tableName)
        {
            const string RowKeyVersionLowerLimit = "0000000000";
            const string RowKeyVersionUpperLimit = "9999999999";

            var context = tableClient.GetDataServiceContext();
            var query = context
                .CreateQuery<EventTableServiceEntity>(tableName)
                .Where(
                    x =>
                    x.RowKey.CompareTo(RowKeyVersionLowerLimit) >= 0 &&
                    x.RowKey.CompareTo(RowKeyVersionUpperLimit) <= 0)
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
    }
}

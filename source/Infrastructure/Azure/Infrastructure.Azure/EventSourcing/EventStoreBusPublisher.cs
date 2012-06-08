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

namespace Infrastructure.Azure.EventSourcing
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Utils;
    using Infrastructure.Util;
    using Microsoft.ServiceBus.Messaging;

    public class EventStoreBusPublisher : IEventStoreBusPublisher
    {
        private readonly IMessageSender sender;
        private readonly IPendingEventsQueue queue;
        private readonly BlockingCollection<string> enqueuedKeys;
        private static readonly int RowKeyPrefixIndex = "Unpublished_".Length;
        private const int MaxDegreeOfParallelism = 10;

        public EventStoreBusPublisher(IMessageSender sender, IPendingEventsQueue queue)
        {
            this.sender = sender;
            this.queue = queue;

            this.enqueuedKeys = new BlockingCollection<string>();
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        Parallel.ForEach(
                            new BlockingCollectionPartitioner<string>(this.enqueuedKeys),
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                                CancellationToken = cancellationToken,
                            },
                            this.ProcessPartition);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                },
                TaskCreationOptions.LongRunning);

            // Query through all partitions to check for pending events, as there could be
            // stored events that were never published before the system was rebooted.
            Task.Factory.StartNew(
                () =>
                {
                    foreach (var partitionKey in this.queue.GetPartitionsWithPendingEvents())
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        this.enqueuedKeys.Add(partitionKey);
                    }
                });
        }

        public void SendAsync(string partitionKey)
        {
            this.enqueuedKeys.Add(partitionKey);
        }

        private void ProcessPartition(string key)
        {
            try
            {
                var pending = this.queue.GetPending(key).AsCachedAnyEnumerable();
                if (pending.Any())
                {
                    foreach (var record in pending)
                    {
                        var item = record;
                        this.sender.Send(() => BuildMessage(item));
                        this.queue.DeletePending(item.PartitionKey, item.RowKey);
                    }
                }
            }
            catch
            {
                // if there was ANY unhandled error, re-add the item to collection.
                // this would allow the main Start logic to potentially have some 
                // recovery logic and retry processing this key if needed. Currently 
                // we're not doing that and the process will just stop and log whatever
                // exception happened.
                this.enqueuedKeys.Add(key);
                throw;
            }
        }

        private static BrokeredMessage BuildMessage(IEventRecord record)
        {
            string version = record.RowKey.Substring(RowKeyPrefixIndex);
            return new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(record.Payload)), true)
            {
                MessageId = record.PartitionKey + "_" + version,
                SessionId = record.SourceId,
                CorrelationId = record.CorrelationId,
                Properties =
                    {
                        { "Version", version },
                        { StandardMetadata.SourceType, record.SourceType },
                        { StandardMetadata.Kind, StandardMetadata.EventKind },
                        { StandardMetadata.AssemblyName, record.AssemblyName },
                        { StandardMetadata.FullName, record.FullName },
                        { StandardMetadata.Namespace, record.Namespace },
                        { StandardMetadata.SourceId, record.SourceId },
                        { StandardMetadata.TypeName, record.TypeName },
                    }
            };
        }
    }
}

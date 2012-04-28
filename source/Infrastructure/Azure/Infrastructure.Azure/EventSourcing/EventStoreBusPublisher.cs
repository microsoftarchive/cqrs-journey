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
    using Infrastructure.Util;
    using Microsoft.ServiceBus.Messaging;

    public class EventStoreBusPublisher : IEventStoreBusPublisher
    {
        private readonly IMessageSender sender;
        private readonly IPendingEventsQueue queue;
        private readonly BlockingCollection<string> enqueuedKeys;
        private static readonly int RowKeyPrefixIndex = "Unpublished_".Length;

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
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                this.ProcessNewPartition(cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                return;
                            }
                        }
                    },
                TaskCreationOptions.LongRunning);

            // TODO: Need to do a query through all partitions to check for pending events, as there could be
            // stored events that were never published before the system was rebooted.
        }

        public void SendAsync(string partitionKey)
        {
            this.enqueuedKeys.Add(partitionKey);
        }

        private void ProcessNewPartition(CancellationToken cancellationToken)
        {
            string key = this.enqueuedKeys.Take(cancellationToken);
            if (key != null)
            {
                // TODO: possible optimization:
                // Each partition could be processed in parallel. The only requirement is that each event within the same partition
                // is sent in the correct order, so parallelization is possible, but be aware of not starting 2 tasks with same partition key.
                try
                {
                    var pending = this.queue.GetPending(key).AsCachedAnyEnumerable();
                    if (pending.Any())
                    {
                        foreach (var record in pending)
                        {
                            var item = record;
                            // There is no way to send all messages in a single transactional batch. Process 1 by 1 synchronously.
                            this.sender.Send(() => BuildMessage(item));
                            this.queue.DeletePending(item.PartitionKey, item.RowKey);
                        }
                    }
                }
                catch
                {
                    // if there was ANY unhandled error, re-add the item to collection.
                    // TODO: Possible enhancement: Catch more specific exceptions and keep retrying after a while
                    this.enqueuedKeys.Add(key);
                    throw;
                }
            }
        }

        private static BrokeredMessage BuildMessage(IEventRecord record)
        {
            string version = record.RowKey.Substring(RowKeyPrefixIndex);
            // TODO: should add SessionID to guarantee ordering.
            // Receiver must be prepared to accept sessions.
            return new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(record.Payload)), true)
            {
                MessageId = record.PartitionKey + "_" + version,
                //SessionId = record.PartitionKey,
                Properties =
                    {
                        { "Version", version },
                        { "SourceType", record.SourceType },
                        { "EventType", record.EventType }
                    }
            };
        }
    }
}

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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;

    public class EventStoreBusPublisher : IEventStoreBusPublisher
    {
        private readonly IMessageSender sender;
        private readonly IPendingEventsQueue queue;
        private readonly BlockingCollection<string> enqueuedKeys;
        private static readonly int RowKeyPrefixIndex = "Unpublished_".Length;
        private const int MaxDegreeOfParallelism = 5;
        private readonly Semaphore throttlingSemaphore;

        public EventStoreBusPublisher(IMessageSender sender, IPendingEventsQueue queue)
        {
            this.sender = sender;
            this.queue = queue;

            this.enqueuedKeys = new BlockingCollection<string>();
            this.throttlingSemaphore = new Semaphore(MaxDegreeOfParallelism, MaxDegreeOfParallelism);
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        foreach (var key in GetThrottlingEnumerable(this.enqueuedKeys.GetConsumingEnumerable(cancellationToken), this.throttlingSemaphore, cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                ProcessPartition(key);
                            }
                            else
                            {
                                this.enqueuedKeys.Add(key);
                                return;
                            }
                        }
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
            IEnumerable<IEventRecord> pending;

            try
            {
                pending = this.queue.GetPending(key);
            }
            catch (Exception e)
            {
                try
                {
                    Trace.TraceError("An error occurred while getting the events pending for publishing for partition {0}:\r\n{1}", key, e);

                    // if there was ANY unhandled error, re-add the item to collection.
                    this.enqueuedKeys.Add(key);

                    // TODO: throttle for some time so we do not retry immediately? shutdown?
                }
                finally
                {
                    this.throttlingSemaphore.Release();
                }

                return;
            }

            var enumerator = pending.GetEnumerator();

            Action sendNextEvent = null;
            Action deletePending = null;
            Action disposeEnumerator = () => { using (enumerator as IDisposable) { } };
            Action<Exception> handleException =
                ex =>
                {
                    try
                    {
                        disposeEnumerator();
                        Trace.TraceError("An error occurred while publishing events for partition {0}:\r\n{1}", key, ex);

                        // if there was ANY unhandled error, re-add the item to collection.
                        this.enqueuedKeys.Add(key);
                    }
                    finally
                    {
                        this.throttlingSemaphore.Release();
                    }

                    // TODO: throttle for some time so we do not retry immediately? shutdown?
                };

            sendNextEvent =
                () =>
                {
                    try
                    {
                        if (enumerator.MoveNext())
                        {
                            var item = enumerator.Current;

                            this.sender.SendAsync(
                                () => BuildMessage(item),
                                deletePending,
                                handleException);
                        }
                        else
                        {
                            // no more elements
                            disposeEnumerator();
                            this.throttlingSemaphore.Release();
                        }
                    }
                    catch (Exception e)
                    {
                        handleException(e);
                    }
                };

            deletePending =
                () =>
                {
                    var item = enumerator.Current;
                    this.queue.DeletePendingAsync(
                        item.PartitionKey,
                        item.RowKey,
                        sendNextEvent,
                        handleException);
                };

            sendNextEvent();
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

        private static IEnumerable<T> GetThrottlingEnumerable<T>(IEnumerable<T> enumerable, Semaphore throttlingSemaphore, CancellationToken cancellationToken)
        {
            throttlingSemaphore.WaitOne();

            foreach (var item in enumerable)
            {
                yield return item;
                throttlingSemaphore.WaitOne();
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
            }
        }
    }
}

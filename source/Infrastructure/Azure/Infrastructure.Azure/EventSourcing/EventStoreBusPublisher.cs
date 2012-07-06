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
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Azure.Instrumentation;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;

    public class EventStoreBusPublisher : IEventStoreBusPublisher, IDisposable
    {
        private readonly IMessageSender sender;
        private readonly IPendingEventsQueue queue;
        private readonly BlockingCollection<string> enqueuedKeys;
        private readonly IEventStoreBusPublisherInstrumentation instrumentation;
        private static readonly int RowKeyPrefixIndex = "Unpublished_".Length;
        private readonly DynamicThrottling dynamicThrottling;

        public EventStoreBusPublisher(IMessageSender sender, IPendingEventsQueue queue, IEventStoreBusPublisherInstrumentation instrumentation)
        {
            this.sender = sender;
            this.queue = queue;
            this.instrumentation = instrumentation;

            this.enqueuedKeys = new BlockingCollection<string>();
            this.dynamicThrottling = 
                new DynamicThrottling(
                    minDegreeOfParallelism: 10,
                    logProductConstant: 20,
                    penalizeFactor: .1,
                    workFailedPenaltyFactor: .25,
                    intervalForRestoringParallelism: 8000);
            this.queue.Retrying += (s, e) => this.dynamicThrottling.Penalize();
            this.sender.Retrying += (s, e) => this.dynamicThrottling.Penalize();
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        foreach (var key in GetThrottlingEnumerable(this.enqueuedKeys.GetConsumingEnumerable(cancellationToken), cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                // TODO: verify if not using Task.Factory.StartNew to process new partition makes this process extremely eager, and consumes all resources when stressed.
                                ProcessPartition(key);
                            }
                            else
                            {
                                this.EnqueueIfNotExists(key);
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

                        this.EnqueueIfNotExists(partitionKey);
                    }
                },
                TaskCreationOptions.LongRunning);

            this.dynamicThrottling.Start(cancellationToken);
        }

        public void SendAsync(string partitionKey, int eventCount)
        {
            if (string.IsNullOrEmpty(partitionKey))
                throw new ArgumentNullException(partitionKey);

            EnqueueIfNotExists(partitionKey);

            this.instrumentation.EventsPublishingRequested(eventCount);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.dynamicThrottling.Dispose();
                this.enqueuedKeys.Dispose();
            }
        }

        private void EnqueueIfNotExists(string partitionKey)
        {
            if (!this.enqueuedKeys.Any(partitionKey.Equals))
            {
                // if the key is not already in the queue, add it.
                this.enqueuedKeys.Add(partitionKey);
            }
        }

        private void ProcessPartition(string key)
        {
            this.instrumentation.EventPublisherStarted();

            this.queue.GetPendingAsync(
                key,
                (results, hasMoreResults) =>
                {
                    var enumerator = results.GetEnumerator();
                    this.SendAndDeletePending(
                        enumerator,
                        allElementWereProcessed =>
                        {
                            enumerator.Dispose();
                            if (!allElementWereProcessed)
                            {
                                this.EnqueueIfNotExists(key);
                            }
                            else if (hasMoreResults)
                            {
                                // if there are more events in this partition, then continue processing and do not mark work as completed.
                                ProcessPartition(key);
                                return;
                            }

                            // all elements were processed or should be retried later. Mark this job as done.
                            this.dynamicThrottling.NotifyWorkCompleted();
                            this.instrumentation.EventPublisherFinished();
                        },
                        ex =>
                        {
                            enumerator.Dispose();
                            Trace.TraceError("An error occurred while publishing events for partition {0}:\r\n{1}", key, ex);

                            // if there was ANY unhandled error, re-add the item to collection.
                            this.EnqueueIfNotExists(key);
                            this.dynamicThrottling.NotifyWorkCompletedWithError();
                            this.instrumentation.EventPublisherFinished();
                        });
                },
                ex =>
                {
                    Trace.TraceError("An error occurred while getting the events pending for publishing for partition {0}:\r\n{1}", key, ex);

                    // if there was ANY unhandled error, re-add the item to collection.
                    this.EnqueueIfNotExists(key);
                    this.dynamicThrottling.NotifyWorkCompletedWithError();
                    this.instrumentation.EventPublisherFinished();
                });
        }

        private void SendAndDeletePending(IEnumerator<IEventRecord> enumerator, Action<bool> successCallback, Action<Exception> errorCallback)
        {

            Action sendNextEvent = null;
            Action deletePending = null;

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
                                errorCallback);
                        }
                        else
                        {
                            // no more elements
                            successCallback(true);
                        }
                    }
                    catch (Exception e)
                    {
                        errorCallback(e);
                    }
                };

            deletePending =
                () =>
                {
                    var item = enumerator.Current;
                    this.queue.DeletePendingAsync(
                        item.PartitionKey,
                        item.RowKey,
                        (bool rowDeleted) =>
                        {
                            if (rowDeleted)
                            {
                                this.instrumentation.EventPublished();

                                sendNextEvent.Invoke();
                            }
                            else
                            {
                                // another thread or process has already sent this event.
                                // stop competing for the same partition and try to send it at the end of the queue if there are any
                                // events still pending.
                                successCallback(false);
                            }
                        },
                        errorCallback);
                };

            sendNextEvent();
        }

        private static BrokeredMessage BuildMessage(IEventRecord record)
        {
            string version = record.RowKey.Substring(RowKeyPrefixIndex);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(record.Payload));
            try
            {
                return new BrokeredMessage(stream, true)
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
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private IEnumerable<T> GetThrottlingEnumerable<T>(IEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            foreach (var item in enumerable)
            {
                this.dynamicThrottling.NotifyWorkStarted();
                yield return item;

                this.dynamicThrottling.WaitUntilAllowedParallelism(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
            }
        }
    }
}

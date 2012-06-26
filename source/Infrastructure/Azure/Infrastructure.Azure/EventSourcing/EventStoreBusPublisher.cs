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
        private readonly DynamicThrottling dynamicThrottling = new DynamicThrottling();

        public EventStoreBusPublisher(IMessageSender sender, IPendingEventsQueue queue, IEventStoreBusPublisherInstrumentation instrumentation)
        {
            this.sender = sender;
            this.queue = queue;
            this.instrumentation = instrumentation;

            this.enqueuedKeys = new BlockingCollection<string>();
            this.queue.Retrying += (s, e) => this.dynamicThrottling.OnRetrying();
            this.sender.Retrying += (s, e) => this.dynamicThrottling.OnRetrying();
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

        public class DynamicThrottling : IDisposable
        {
            // configuration

            /// <summary>
            /// Maximum number of parallel jobs.
            /// </summary>
            private const int MaxDegreeOfParallelism = 230;

            /// <summary>
            /// Minimum number of parallel jobs.
            /// </summary>
            private const int MinDegreeOfParallelism = 30;
            
            /// <summary>
            /// Number of degrees of parallelism to remove on retrying.
            /// </summary>
            private const int RetryParallelismPenalty = 3;

            /// <summary>
            /// Number of degrees of parallelism to remove when work fails.
            /// </summary>
            private const int WorkFailedParallelismPenalty = 10;

            /// <summary>
            /// Number of degrees of parallelism to restore on work completed.
            /// </summary>
            private const int WorkCompletedParallelismGain = 1;

            /// <summary>
            /// Interval in milliseconds to restore 1 degree of parallelism.
            /// </summary>
            private const int IntervalForRestoringDegreeOfParallelism = 8000;


            private readonly AutoResetEvent waitHandle = new AutoResetEvent(true);
            private readonly Timer parallelismRestoringTimer;

            private int currentParallelJobs = 0;
            private int availableDegreesOfParallelism = MaxDegreeOfParallelism;

            public DynamicThrottling()
            {
                this.parallelismRestoringTimer = new Timer(s => this.IncrementDegreesOfParallelism(1));
            }

            public void WaitUntilAllowedParallelism(CancellationToken cancellationToken)
            {
                while (this.currentParallelJobs >= this.availableDegreesOfParallelism)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    // Trace.WriteLine("Waiting for available degrees of parallelism. Available: " + this.availableDegreesOfParallelism + ". In use: " + this.currentParallelJobs);

                    this.waitHandle.WaitOne();
                }
            }

            public void NotifyWorkCompleted()
            {
                Interlocked.Decrement(ref this.currentParallelJobs);
                // Trace.WriteLine("Job finished. Parallel jobs are now: " + this.currentParallelJobs);
                IncrementDegreesOfParallelism(WorkCompletedParallelismGain);
            }

            public void NotifyWorkStarted()
            {
                Interlocked.Increment(ref this.currentParallelJobs);
                // Trace.WriteLine("Job started. Parallel jobs are now: " + this.currentParallelJobs);
            }

            public void OnRetrying()
            {
                // Slightly penalize with removal of some degrees of parallelism.
                this.DecrementDegreesOfParallelism(RetryParallelismPenalty);
            }

            public void NotifyWorkCompletedWithError()
            {
                // Largely penalize with removal of several degrees of parallelism.
                this.DecrementDegreesOfParallelism(WorkFailedParallelismPenalty);
                Interlocked.Decrement(ref this.currentParallelJobs);
                // Trace.WriteLine("Job finished with error. Parallel jobs are now: " + this.currentParallelJobs);
            }

            public void Start(CancellationToken cancellationToken)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() => this.parallelismRestoringTimer.Change(Timeout.Infinite, Timeout.Infinite));
                }

                this.parallelismRestoringTimer.Change(IntervalForRestoringDegreeOfParallelism, IntervalForRestoringDegreeOfParallelism);
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
                    this.waitHandle.Dispose();
                    this.parallelismRestoringTimer.Dispose();
                }
            }

            private void IncrementDegreesOfParallelism(int count)
            {
                if (this.availableDegreesOfParallelism < MaxDegreeOfParallelism)
                {
                    this.availableDegreesOfParallelism += count;
                    if (this.availableDegreesOfParallelism >= MaxDegreeOfParallelism)
                    {
                        this.availableDegreesOfParallelism = MaxDegreeOfParallelism;
                        // Trace.WriteLine("Incremented available degrees of parallelism. Available: " + this.availableDegreesOfParallelism);
                    }
                }

                this.waitHandle.Set();
            }

            private void DecrementDegreesOfParallelism(int count)
            {
                this.availableDegreesOfParallelism -= count;
                if (this.availableDegreesOfParallelism < MinDegreeOfParallelism)
                {
                    this.availableDegreesOfParallelism = MinDegreeOfParallelism;
                }
                // Trace.WriteLine("Decremented available degrees of parallelism. Available: " + this.availableDegreesOfParallelism);
            }
        }
    }
}

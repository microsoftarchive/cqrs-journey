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

// Based on http://windowsazurecat.com/2011/09/best-practices-leveraging-windows-azure-service-bus-brokered-messaging-api/

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Azure.Instrumentation;
    using Infrastructure.Azure.Utils;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Implements an asynchronous receiver of messages from a Windows Azure 
    /// Service Bus topic subscription.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In V3 we made a lot of changes to optimize the performance and scalability of the receiver.
    /// See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see> for more information on the optimizations and migration to V3.
    /// </para>
    /// <para>
    /// The current implementation uses async calls to communicate with the service bus, although the message processing is done with a blocking synchronous call.
    /// We could still make several performance improvements. For example, we could react to system-wide throttling indicators to avoid overwhelming
    /// the services when under heavy load. See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see> for more potential 
    /// performance and scalability optimizations.
    /// </para>
    /// </remarks>
    public class SubscriptionReceiver : IMessageReceiver, IDisposable
    {
        private static readonly TimeSpan ReceiveLongPollingTimeout = TimeSpan.FromMinutes(1);

        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly ServiceBusSettings settings;
        private readonly string topic;
        private readonly ISubscriptionReceiverInstrumentation instrumentation;
        private string subscription;
        private readonly object lockObject = new object();
        private readonly Microsoft.Practices.TransientFaultHandling.RetryPolicy receiveRetryPolicy;
        private readonly bool processInParallel;
        private readonly DynamicThrottling dynamicThrottling;
        private CancellationTokenSource cancellationSource;
        private SubscriptionClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReceiver"/> class, 
        /// automatically creating the topic and subscription if they don't exist.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Instrumentation disabled in this overload")]
        public SubscriptionReceiver(ServiceBusSettings settings, string topic, string subscription, bool processInParallel = false)
            : this(
                settings,
                topic,
                subscription,
                processInParallel,
                new SubscriptionReceiverInstrumentation(subscription, false),
                new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReceiver"/> class, 
        /// automatically creating the topic and subscription if they don't exist.
        /// </summary>
        public SubscriptionReceiver(ServiceBusSettings settings, string topic, string subscription, bool processInParallel, ISubscriptionReceiverInstrumentation instrumentation)
            : this(
                settings,
                topic,
                subscription,
                processInParallel,
                instrumentation,
                new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReceiver"/> class, 
        /// automatically creating the topic and subscription if they don't exist.
        /// </summary>
        protected SubscriptionReceiver(ServiceBusSettings settings, string topic, string subscription, bool processInParallel, ISubscriptionReceiverInstrumentation instrumentation, RetryStrategy backgroundRetryStrategy)
        {
            this.settings = settings;
            this.topic = topic;
            this.subscription = subscription;
            this.processInParallel = processInParallel;
            this.instrumentation = instrumentation;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            var messagingFactory = MessagingFactory.Create(this.serviceUri, tokenProvider);
            this.client = messagingFactory.CreateSubscriptionClient(topic, subscription);
            if (this.processInParallel)
            {
                this.client.PrefetchCount = 18;
            }
            else
            {
                this.client.PrefetchCount = 14;
            }

            this.dynamicThrottling =
                new DynamicThrottling(
                    maxDegreeOfParallelism: 100,
                    minDegreeOfParallelism: 50,
                    penaltyAmount: 3,
                    workFailedPenaltyAmount: 5,
                    workCompletedParallelismGain: 1,
                    intervalForRestoringDegreeOfParallelism: 8000);
            this.receiveRetryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(backgroundRetryStrategy);
            this.receiveRetryPolicy.Retrying += (s, e) =>
            {
                this.dynamicThrottling.Penalize();
                Trace.TraceWarning(
                    "An error occurred in attempt number {1} to receive a message from subscription {2}: {0}",
                    e.LastException.Message,
                    e.CurrentRetryCount,
                    this.subscription);
            };
        }

        /// <summary>
        /// Handler for incoming messages. The return value indicates whether the message should be disposed.
        /// </summary>
        protected Func<BrokeredMessage, MessageReleaseAction> MessageHandler { get; private set; }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public void Start(Func<BrokeredMessage, MessageReleaseAction> messageHandler)
        {
            lock (this.lockObject)
            {
                this.MessageHandler = messageHandler;
                this.cancellationSource = new CancellationTokenSource();
                Task.Factory.StartNew(() =>
                    this.ReceiveMessages(this.cancellationSource.Token),
                    this.cancellationSource.Token);
                this.dynamicThrottling.Start(this.cancellationSource.Token);
            }
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public void Stop()
        {
            lock (this.lockObject)
            {
                using (this.cancellationSource)
                {
                    if (this.cancellationSource != null)
                    {
                        this.cancellationSource.Cancel();
                        this.cancellationSource = null;
                        this.MessageHandler = null;
                    }
                }
            }
        }

        /// <summary>
        /// Stops the listener if it was started previously.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Stop();

            if (disposing)
            {
                using (this.instrumentation as IDisposable) { }
                using (this.dynamicThrottling as IDisposable) { }
            }
        }

        protected virtual MessageReleaseAction InvokeMessageHandler(BrokeredMessage message)
        {
            return this.MessageHandler != null ? this.MessageHandler(message) : MessageReleaseAction.AbandonMessage;
        }

        ~SubscriptionReceiver()
        {
            Dispose(false);
        }

        /// <summary>
        /// Receives the messages in an endless asynchronous loop.
        /// </summary>
        private void ReceiveMessages(CancellationToken cancellationToken)
        {
            // Declare an action to receive the next message in the queue or end if cancelled.
            Action receiveNext = null;

            // Declare an action acting as a callback whenever a non-transient exception occurs while receiving or processing messages.
            Action<Exception> recoverReceive = null;

            // Declare an action responsible for the core operations in the message receive loop.
            Action receiveMessage = (() =>
            {
                // Use a retry policy to execute the Receive action in an asynchronous and reliable fashion.
                this.receiveRetryPolicy.ExecuteAction
                (
                    cb =>
                    {
                        // Start receiving a new message asynchronously.
                        this.client.BeginReceive(ReceiveLongPollingTimeout, cb, null);
                    },
                    ar =>
                    {
                        // Complete the asynchronous operation. This may throw an exception that will be handled internally by retry policy.
                        try
                        {
                            return this.client.EndReceive(ar);
                        }
                        catch (TimeoutException)
                        {
                            // TimeoutException is not just transient but completely expected in this case, so not relying on Topaz to retry
                            return null;
                        }
                    },
                    msg =>
                    {
                        // Process the message once it was successfully received
                        if (this.processInParallel)
                        {
                            // Continue receiving and processing new messages asynchronously
                            Task.Factory.StartNew(receiveNext);
                        }

                        // Check if we actually received any messages.
                        if (msg != null)
                        {
                            var roundtripStopwatch = Stopwatch.StartNew();
                            long schedulingElapsedMilliseconds = 0;
                            long processingElapsedMilliseconds = 0;

                            Task.Factory.StartNew(() =>
                                {
                                    var releaseAction = MessageReleaseAction.AbandonMessage;

                                    try
                                    {
                                        this.instrumentation.MessageReceived();

                                        schedulingElapsedMilliseconds = roundtripStopwatch.ElapsedMilliseconds;

                                        // Make sure the process was told to stop receiving while it was waiting for a new message.
                                        if (!cancellationToken.IsCancellationRequested)
                                        {
                                            try
                                            {
                                                try
                                                {
                                                    // Process the received message.
                                                    releaseAction = this.InvokeMessageHandler(msg);

                                                    processingElapsedMilliseconds = roundtripStopwatch.ElapsedMilliseconds - schedulingElapsedMilliseconds;
                                                    this.instrumentation.MessageProcessed(releaseAction.Kind == MessageReleaseActionKind.Complete, processingElapsedMilliseconds);
                                                }
                                                catch
                                                {
                                                    processingElapsedMilliseconds = roundtripStopwatch.ElapsedMilliseconds - schedulingElapsedMilliseconds;
                                                    this.instrumentation.MessageProcessed(false, processingElapsedMilliseconds);

                                                    throw;
                                                }
                                            }
                                            finally
                                            {
                                                if (roundtripStopwatch.Elapsed > TimeSpan.FromSeconds(45))
                                                {
                                                    this.dynamicThrottling.Penalize();
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        // Ensure that any resources allocated by a BrokeredMessage instance are released.
                                        this.ReleaseMessage(msg, releaseAction, processingElapsedMilliseconds, schedulingElapsedMilliseconds, roundtripStopwatch);
                                    }

                                    if (!this.processInParallel)
                                    {
                                        // Continue receiving and processing new messages until told to stop.
                                        receiveNext.Invoke();
                                    }
                                });
                        }
                        else
                        {
                            this.dynamicThrottling.NotifyWorkCompleted();
                            if (!this.processInParallel)
                            {
                                // Continue receiving and processing new messages until told to stop.
                                receiveNext.Invoke();
                            }
                        }
                    },
                    ex =>
                    {
                        // Invoke a custom action to indicate that we have encountered an exception and
                        // need further decision as to whether to continue receiving messages.
                        recoverReceive.Invoke(ex);
                    });
            });

            // Initialize an action to receive the next message in the queue or end if cancelled.
            receiveNext = () =>
            {
                this.dynamicThrottling.WaitUntilAllowedParallelism(cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    this.dynamicThrottling.NotifyWorkStarted();
                    // Continue receiving and processing new messages until told to stop.
                    receiveMessage.Invoke();
                }
            };

            // Initialize a custom action acting as a callback whenever a non-transient exception occurs while receiving or processing messages.
            recoverReceive = ex =>
            {
                // Just log an exception. Do not allow an unhandled exception to terminate the message receive loop abnormally.
                Trace.TraceError("An unrecoverable error occurred while trying to receive a new message from subscription {1}:\r\n{0}", ex, this.subscription);
                this.dynamicThrottling.NotifyWorkCompletedWithError();

                if (!cancellationToken.IsCancellationRequested)
                {
                    // Continue receiving and processing new messages until told to stop regardless of any exceptions.
                    TaskEx.Delay(10000).ContinueWith(t => receiveMessage.Invoke());
                }
            };

            // Start receiving messages asynchronously.
            receiveNext.Invoke();
        }

        private void ReleaseMessage(BrokeredMessage msg, MessageReleaseAction releaseAction, long processingElapsedMilliseconds, long schedulingElapsedMilliseconds, Stopwatch roundtripStopwatch)
        {
            switch (releaseAction.Kind)
            {
                case MessageReleaseActionKind.Complete:
                    msg.SafeCompleteAsync(
                        this.subscription,
                        success =>
                        {
                            msg.Dispose();
                            this.instrumentation.MessageCompleted(success);
                            if (success)
                            {
                                this.dynamicThrottling.NotifyWorkCompleted();
                            }
                            else
                            {
                                this.dynamicThrottling.NotifyWorkCompletedWithError();
                            }
                        },
                        processingElapsedMilliseconds,
                        schedulingElapsedMilliseconds,
                        roundtripStopwatch);
                    break;
                case MessageReleaseActionKind.Abandon:
                    msg.SafeAbandonAsync(
                        this.subscription,
                        success => { msg.Dispose(); this.instrumentation.MessageCompleted(false); this.dynamicThrottling.NotifyWorkCompletedWithError(); },
                        processingElapsedMilliseconds,
                        schedulingElapsedMilliseconds,
                        roundtripStopwatch);
                    break;
                case MessageReleaseActionKind.DeadLetter:
                    msg.SafeDeadLetterAsync(
                        this.subscription,
                        releaseAction.DeadLetterReason,
                        releaseAction.DeadLetterDescription,
                        success => { msg.Dispose(); this.instrumentation.MessageCompleted(false); this.dynamicThrottling.NotifyWorkCompletedWithError(); },
                        processingElapsedMilliseconds,
                        schedulingElapsedMilliseconds,
                        roundtripStopwatch);
                    break;
                default:
                    break;
            }
        }
    }
}

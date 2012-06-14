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

// Based on http://windowsazurecat.com/2011/09/best-practices-leveraging-windows-azure-service-bus-brokered-messaging-api/

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Azure.Utils;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Implements an asynchronous receiver of messages from an Azure 
    /// service bus topic subscription.
    /// </summary>
    public class SubscriptionReceiver : IMessageReceiver, IDisposable
    {
        private static readonly TimeSpan ReceiveLongPollingTimeout = TimeSpan.FromMinutes(1);

        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly ServiceBusSettings settings;
        private readonly string topic;
        private string subscription;
        private readonly object lockObject = new object();
        private readonly Microsoft.Practices.TransientFaultHandling.RetryPolicy receiveRetryPolicy;
        private CancellationTokenSource cancellationSource;
        private SubscriptionClient client;

        /// <summary>
        /// Event raised whenever a message is received. Consumer of 
        /// the event is responsible for disposing the message when 
        /// appropriate.
        /// </summary>
        public event EventHandler<BrokeredMessageEventArgs> MessageReceived = (sender, args) => { };

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReceiver"/> class, 
        /// automatically creating the topic and subscription if they don't exist.
        /// </summary>
        public SubscriptionReceiver(ServiceBusSettings settings, string topic, string subscription)
            : this(
                settings,
                topic,
                subscription,
                new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReceiver"/> class, 
        /// automatically creating the topic and subscription if they don't exist.
        /// </summary>
        protected SubscriptionReceiver(ServiceBusSettings settings, string topic, string subscription, RetryStrategy backgroundRetryStrategy)
        {
            this.settings = settings;
            this.topic = topic;
            this.subscription = subscription;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            var messagingFactory = MessagingFactory.Create(this.serviceUri, tokenProvider);
            this.client = messagingFactory.CreateSubscriptionClient(topic, subscription);
            this.client.PrefetchCount = 50;

            this.receiveRetryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(backgroundRetryStrategy);
            this.receiveRetryPolicy.Retrying += (s, e) =>
            {
                Trace.TraceWarning(
                    "An error occurred in attempt number {1} to receive a message from subscription {2}: {0}",
                    e.LastException.Message,
                    e.CurrentRetryCount,
                    this.subscription);
            };
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public void Start()
        {
            lock (this.lockObject)
            {
                this.cancellationSource = new CancellationTokenSource();
                Task.Factory.StartNew(() =>
                    this.ReceiveMessages(this.cancellationSource.Token),
                    this.cancellationSource.Token);
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
            // Declare an action acting as a callback whenever a message arrives on a queue.
            Action completeReceive = null;

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
                            catch(TimeoutException)
                            {
                                // TimeoutException is not just transient but completely expected in this case, so not relying on Topaz to retry
                                return null;
                            }
                        },
                    msg =>
                    {
                        // Process the message once it was successfully received
                        BrokeredMessageEventArgs args = null;

                        // Check if we actually received any messages.
                        if (msg != null)
                        {
                            // Make sure we are not told to stop receiving while we were waiting for a new message.
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    // Process the received message.
                                    args = new BrokeredMessageEventArgs(msg);
                                    this.MessageReceived(this, args);

                                    // TODO: code commented out because the IMessageProcessor is in charge of Completing the message
                                    //// With PeekLock mode, we should mark the processed message as completed.
                                    //if (this.client.Mode == ReceiveMode.PeekLock)
                                    //{
                                    //    // Mark brokered message as completed at which point it's removed from the queue.
                                    //    msg.SafeComplete();
                                    //}
                                }
                                //catch
                                //{
                                //    // With PeekLock mode, we should mark the failed message as abandoned.
                                //    if (this.client.Mode == ReceiveMode.PeekLock)
                                //    {
                                //        // Abandons a brokered message. This will cause Service Bus to unlock the message and make it available 
                                //        // to be received again, either by the same consumer or by another completing consumer.
                                //        msg.SafeAbandon();
                                //    }
                                //}
                                finally
                                {
                                    // Ensure that any resources allocated by a BrokeredMessage instance are released.
                                    if (args == null || !args.DoNotDisposeMessage)
                                    {
                                        // Ensure that any resources allocated by a BrokeredMessage instance are released.
                                        msg.Dispose();
                                    }
                                }
                            }
                            else
                            {
                                // If we were told to stop processing, the current message needs to be unlocked and return back to the queue.
                                if (this.client.Mode == ReceiveMode.PeekLock)
                                {
                                    msg.SafeAbandon();
                                }
                            }
                        }
                        
                        // Continue receiving and processing new messages until we are told to stop.
                        completeReceive.Invoke();
                    },
                    ex =>
                    {
                        // Invoke a custom action to indicate that we have encountered an exception and
                        // need further decision as to whether to continue receiving messages.
                        recoverReceive.Invoke(ex);
                    });
            });

            // Initialize a custom action acting as a callback whenever a message arrives on a queue.
            completeReceive = () =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    // Continue receiving and processing new messages until we are told to stop.
                    receiveMessage.Invoke();
                }
            };

            // Initialize a custom action acting as a callback whenever a non-transient exception occurs while receiving or processing messages.
            recoverReceive = ex =>
            {
                // Just log an exception. Do not allow an unhandled exception to terminate the message receive loop abnormally.
                Trace.TraceError("An unrecoverable error occurred while trying to receive a new message from subscription {1}:\r\n{0}", ex, this.subscription);

                if (!cancellationToken.IsCancellationRequested)
                {
                    // Continue receiving and processing new messages until we are told to stop regardless of any exceptions.
                    receiveMessage.Invoke();
                }
            };

            // Start receiving messages asynchronously.
            receiveMessage.Invoke();
        }
    }
}

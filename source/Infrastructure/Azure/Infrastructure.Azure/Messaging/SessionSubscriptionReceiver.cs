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

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Implements an asynchronous receiver of messages from an Azure 
    /// service bus topic subscription using sessions.
    /// </summary>
    public class SessionSubscriptionReceiver : IMessageReceiver, IDisposable
    {
        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly MessagingSettings settings;
        private readonly string topic;
        private string subscription;
        private readonly object lockObject = new object();
        private readonly RetryPolicy initializationRetryPolicy;
        private readonly RetryPolicy receiveRetryPolicy;
        private CancellationTokenSource cancellationSource;
        private SubscriptionClient client;
        private readonly NamespaceManager namespaceManager;

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
        public SessionSubscriptionReceiver(MessagingSettings settings, string topic, string subscription)
            : this(
                settings,
                topic,
                subscription,
                new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1)),
                new Incremental(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionReceiver"/> class, 
        /// automatically creating the topic and subscription if they don't exist.
        /// </summary>
        protected SessionSubscriptionReceiver(MessagingSettings settings, string topic, string subscription, RetryStrategy backgroundRetryStrategy, RetryStrategy blockingRetryStrategy)
        {
            this.settings = settings;
            this.topic = topic;
            this.subscription = subscription;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            var messagingFactory = MessagingFactory.Create(this.serviceUri, tokenProvider);
            this.client = messagingFactory.CreateSubscriptionClient(topic, subscription);

            this.namespaceManager = new NamespaceManager(this.serviceUri, this.tokenProvider);

            this.initializationRetryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(blockingRetryStrategy);
            this.receiveRetryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(backgroundRetryStrategy);
            this.receiveRetryPolicy.Retrying +=
                (s, e) =>
                {
                    Trace.TraceError("An error occurred in attempt number {1} to receive a message: {0}", e.LastException.Message, e.CurrentRetryCount);
                };


            this.initializationRetryPolicy.ExecuteAction(CreateTopicIfNotExists);
            this.initializationRetryPolicy.ExecuteAction(CreateSubscriptionIfNotExists);
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public void Start()
        {
            lock (this.lockObject)
            {
                // If it's not null, there is already a listening task.
                if (this.cancellationSource == null)
                {
                    this.cancellationSource = new CancellationTokenSource();
                    Task.Factory.StartNew(() =>
                        this.ReceiveMessages(this.cancellationSource.Token),
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Current);
                }
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

        ~SessionSubscriptionReceiver()
        {
            Dispose(false);
        }

        /// <summary>
        /// Receives the messages in an endless loop.
        /// </summary>
        private void ReceiveMessages(CancellationToken cancellationToken)
        {
            MessageSession session = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    session = this.receiveRetryPolicy.ExecuteAction<MessageSession>(() => this.client.AcceptMessageSession(TimeSpan.FromSeconds(10)));
                }
                catch (Exception e)
                {
                    Trace.TraceError("An unrecoverable error occurred while trying to accept a new message session:\r\n{0}", e);

                    throw;
                }

                if (session == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                BrokeredMessage message = null;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        try
                        {
                            // NOTE: we don't long-poll more than a few seconds as 
                            // we're already on a background thread and we want to 
                            // allow other threads/processes/machines to potentially 
                            // receive messages too.
                            message = this.receiveRetryPolicy.ExecuteAction<BrokeredMessage>(() => session.Receive(TimeSpan.FromSeconds(10)));
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError("An unrecoverable error occurred while trying to receive a new message:\r\n{0}", e);

                            throw;
                        }

                        if (message == null)
                        {
                            // If we have no more messages for this session, exit and try another.
                            break;
                        }

                        this.MessageReceived(this, new BrokeredMessageEventArgs(message));
                    }
                    finally
                    {
                        if (message != null)
                        {
                            message.Dispose();
                        }
                    }
                }

                if (session != null)
                {
                    this.receiveRetryPolicy.ExecuteAction(() => session.Close());
                }
            }
        }

        private void CreateTopicIfNotExists()
        {
            var topicDescription =
                new TopicDescription(this.topic)
                {
                    RequiresDuplicateDetection = true,
                    DuplicateDetectionHistoryTimeWindow = TimeSpan.FromHours(1)
                };
            try
            {
                this.namespaceManager.CreateTopic(topicDescription);
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }

        private void CreateSubscriptionIfNotExists()
        {
            try
            {
                this.namespaceManager.CreateSubscription(new SubscriptionDescription(this.topic, this.subscription) { RequiresSession = true });
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }
    }
}

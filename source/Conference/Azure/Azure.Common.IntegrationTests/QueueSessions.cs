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

namespace Azure.IntegrationTests
{
    using System;
    using System.IO;
    using System.ServiceModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging;
    using Common;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    public class QueueSessions
    {
        public QueueSessions()
        {
            this.Settings = MessagingSettings.Read("Settings.xml");
        }

        public MessagingSettings Settings { get; private set; }

        [Fact]
        public void when_starting_session_then_can_correlate()
        {
            var uri = ServiceBusEnvironment.CreateServiceUri(this.Settings.ServiceUriScheme, this.Settings.ServiceNamespace, this.Settings.ServicePath);
            var token = TokenProvider.CreateSharedSecretTokenProvider(this.Settings.TokenIssuer, this.Settings.TokenAccessKey);

            var factory = MessagingFactory.Create(uri, token);
            var ns = new NamespaceManager(uri, token);

            try
            {
                ns.DeleteQueue("queue-session-state");
            }
            catch (MessagingEntityNotFoundException)
            {
            }

            try
            {
                ns.CreateQueue(new QueueDescription("queue-session-state") { RequiresSession = true });
            }
            catch (MessagingEntityAlreadyExistsException)
            {
            }

            var client = factory.CreateQueueClient("queue-session-state");
            var sessionId = Guid.NewGuid().ToString();
            factory.CreateQueueClient("queue-session-state")
                .Send(new BrokeredMessage("Hello") { SessionId = sessionId });

            var session = client.AcceptMessageSession();
            BrokeredMessage message = null;

            Task.Factory.StartNew(() =>
            {
                message = session.Receive();
                message.Complete();
            });

            while (message == null)
            {
                Thread.Sleep(50);
            }

            Assert.Equal(sessionId, message.SessionId);
            Assert.Equal("Hello", message.GetBody<string>());

            session.Close();
        }

        [Fact]
        public void when_end_to_end_then_succeeds()
        {
            var queueName = "queue-sessions-workflows";
            var serializer = new BinarySerializer();

            var eventBus = new EventBus(new TopicSender(this.Settings, "queue-sessions-events"), new MetadataProvider(), serializer);
            var workflowBus = new WorkflowBus(new QueueSender(this.Settings, queueName), new MetadataProvider(), serializer);

            var workflow = default(RegistrationWorkflow);
            var correlationId = Guid.NewGuid();
            var store = new WorkflowStore(this.Settings, queueName, serializer);

            var eventProcessor = new EventProcessor(new SubscriptionReceiver(this.Settings, "queue-sessions-events", "all"), serializer);

            // Forwards events that have correlation ids to the workflow bus.
            eventProcessor.Register(new DelegateEventHandler<ICorrelatedEvent>(e =>
                {
                    if (e.CorrelationId != Guid.Empty)
                    {
                        Console.WriteLine("Forwarding event {0}", e);
                        workflowBus.Publish(e);
                    }
                }));
            // Creates the workflow.
            eventProcessor.Register(new DelegateEventHandler<OrderPlaced>(e =>
                {
                    workflow = new RegistrationWorkflow(correlationId);
                    Console.WriteLine("Created workflow {0}", correlationId);
                    store.Save(workflow);
                }));

            // This is the handler that has no way of knowing the correlation 
            // or all the interested workflows unless it keeps a table of 
            // "global" events (non-workflow originated) and the destination 
            // workflow instances to dispatch to. Maybe an Azure Table?
            eventProcessor.Register(new DelegateEventHandler<UserDeactivated>(e =>
            {
                foreach (var id in /* lookup correlations */ new[] { correlationId })
                {
                    Console.WriteLine("This is where we'd lookup workflow ids (i.e. {0})", correlationId);
                    // We'd set each of the correlations we found, and forward to each.
                    e.CorrelationId = id;
                    workflowBus.Publish(e);
                }
            }));

            eventProcessor.Start();

            var workflowProcessor = new WorkflowProcessor(this.Settings, queueName, serializer);
            workflowProcessor.Start();

            var orderId = Guid.NewGuid();

            eventBus.Publish(new OrderPlaced { OrderId = orderId });

            // Wait for it to be processed.
            while ((workflow = store.Find<RegistrationWorkflow>(correlationId)) == null)
            {
                Thread.Sleep(50);
            }

            eventBus.Publish(new ReservationMade
            {
                CorrelationId = correlationId,
                OrderId = orderId,
                SeatType = Guid.NewGuid(),
                Quantity = 10
            });

            while ((workflow = store.Find<RegistrationWorkflow>(correlationId)).State != RegistrationWorkflow.States.Reserved)
            {
                Thread.Sleep(50);
            }

            eventBus.Publish(new UserDeactivated());

            while ((workflow = store.Find<RegistrationWorkflow>(correlationId)) == null ||
                workflow.State != RegistrationWorkflow.States.Completed)
            {
                Thread.Sleep(50);
            }
        }

        public class WorkflowProcessor : IDisposable
        {
            private readonly TokenProvider tokenProvider;
            private readonly Uri serviceUri;
            private readonly MessagingSettings settings;
            private CancellationTokenSource cancellationSource;
            private QueueClient client;
            private object lockObject = new object();
            private ISerializer serializer;

            public WorkflowProcessor(MessagingSettings settings, string queueName, ISerializer serializer)
            {
                this.settings = settings;
                this.serializer = serializer;

                this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
                this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

                var messagingFactory = MessagingFactory.Create(this.serviceUri, tokenProvider);
                this.client = messagingFactory.CreateQueueClient(queueName);

                var ns = new NamespaceManager(this.serviceUri, this.tokenProvider);

                try
                {
#if DEBUG
                    // Just for debugging/testing, always drop/recreate.
                    ns.DeleteQueue(queueName);
#endif
                }
                catch (MessagingEntityNotFoundException)
                {
                }

                try
                {
                    ns.CreateQueue(new QueueDescription(queueName)
                    {
                        RequiresDuplicateDetection = true,
                        DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(30),
                        RequiresSession = true,
                    });
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                }
            }

            public void Start()
            {
                lock (this.lockObject)
                {
                    this.cancellationSource = new CancellationTokenSource();
                    Task.Factory.StartNew(() => this.ReceiveMessages(this.cancellationSource.Token), this.cancellationSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                }
            }

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

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                this.Stop();
            }

            ~WorkflowProcessor()
            {
                Dispose(false);
            }

            private void ReceiveMessages(CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var session = default(MessageSession);
                    try
                    {
                        session = this.client.AcceptMessageSession(TimeSpan.FromSeconds(10));
                    }
                    catch (TimeoutException) { }
                    catch (SessionCannotBeLockedException) { }
                    catch (CommunicationException) { }

                    if (session == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            var message = default(BrokeredMessage);
                            try
                            {
                                message = session.Receive(TimeSpan.FromSeconds(10));
                            }
                            catch (TimeoutException)
                            {
                            }

                            if (message == null)
                            {
                                session.Close();
                                Thread.Sleep(100);
                                continue;
                            }

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                var stream = session.GetState();
                                if (stream != null)
                                {
                                    var workflow = (dynamic)this.serializer.Deserialize(stream);
                                    var @event = (dynamic)this.serializer.Deserialize(message.GetBody<Stream>());

                                    Console.WriteLine("Calling event handler on workflow {0}", workflow);
                                    workflow.Handle(@event);

                                    stream = new MemoryStream();
                                    this.serializer.Serialize(stream, workflow);
                                    stream.Position = 0;
                                    session.SetState(stream);
                                }
                            }

                            message.Complete();
                        }
                    }
                    finally
                    {
                        if (!session.IsClosed)
                            session.Close();
                    }
                }
            }
        }

        public class WorkflowStore
        {
            private TokenProvider tokenProvider;
            private Uri serviceUri;
            private string queueName;
            private ISerializer serializer;

            public WorkflowStore(MessagingSettings settings, string queueName, ISerializer serializer)
            {
                this.queueName = queueName;
                this.serializer = serializer;
                this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);
                this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);

                var ns = new NamespaceManager(this.serviceUri, this.tokenProvider);

                try
                {
#if DEBUG
                    // Just for debugging/testing, always drop/recreate.
                    ns.DeleteQueue(queueName);
#endif
                }
                catch (MessagingEntityNotFoundException)
                {
                }

                try
                {
                    ns.CreateQueue(new QueueDescription(queueName)
                    {
                        RequiresDuplicateDetection = true,
                        DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(30),
                        RequiresSession = true,
                    });
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                }
            }

            public void Save<T>(T workflow)
                where T : IIdentifiable
            {
                var factory = MessagingFactory.Create(this.serviceUri, this.tokenProvider);
                var client = factory.CreateQueueClient(this.queueName);

                var session = default(MessageSession);
                while (session == null)
                {
                    try
                    {
                        session = client.AcceptMessageSession(workflow.Id.ToString(), TimeSpan.FromSeconds(5));
                    }
                    catch (TimeoutException) { }
                    catch (SessionCannotBeLockedException) { }
                    catch (CommunicationException) { }
                }

                try
                {
                    var stream = new MemoryStream();
                    this.serializer.Serialize(stream, workflow);
                    stream.Position = 0;

                    session.SetState(stream);
                }
                finally
                {
                    if (!session.IsClosed)
                        session.Close();
                }
            }

            public T Find<T>(Guid id)
                where T : IIdentifiable
            {
                var factory = MessagingFactory.Create(this.serviceUri, this.tokenProvider);
                var client = factory.CreateQueueClient(this.queueName);

                var session = default(MessageSession);
                while (session == null)
                {
                    try
                    {
                        session = client.AcceptMessageSession(id.ToString(), TimeSpan.FromSeconds(5));
                    }
                    catch (TimeoutException) { }
                    catch (SessionCannotBeLockedException) { }
                    catch (CommunicationException) { }
                }

                try
                {
                    var stream = session.GetState();
                    if (stream == null)
                        return default(T);

                    return (T)this.serializer.Deserialize(stream);
                }
                finally
                {
                    if (!session.IsClosed)
                        session.Close();
                }
            }
        }

        public class WorkflowBus : EventBus
        {
            public WorkflowBus(IMessageSender sender, IMetadataProvider metadata, ISerializer serializer)
                : base(sender, metadata, serializer)
            {
            }

            protected override BrokeredMessage BuildMessage(IEvent @event)
            {
                var message = base.BuildMessage(@event);

                var correlated = @event as ICorrelatedEvent;
                if (correlated != null)
                    message.SessionId = correlated.CorrelationId.ToString();

                return message;
            }
        }

        public interface IIdentifiable
        {
            Guid Id { get; }
        }

        [Serializable]
        public class RegistrationWorkflow : IIdentifiable,
            IEventHandler<OrderPlaced>,
            IEventHandler<ReservationMade>,
            IEventHandler<UserDeactivated>
        {
            public enum States
            {
                New,
                Started,
                Reserved,
                Completed,
            }

            public RegistrationWorkflow(Guid id)
            {
                this.Id = id;
            }

            public Guid Id { get; set; }
            public States State { get; set; }

            public void Handle(OrderPlaced @event)
            {
                this.State = States.Started;
            }

            public void Handle(ReservationMade @event)
            {
                this.State = States.Reserved;
            }

            public void Handle(UserDeactivated @event)
            {
                this.State = States.Completed;
            }
        }

        [Serializable]
        public class OrderPlaced : IEvent
        {
            public Guid OrderId { get; set; }
        }

        [Serializable]
        public class ReservationMade : IEvent, ICorrelatedEvent
        {
            public Guid CorrelationId { get; set; }

            public Guid OrderId { get; set; }
            public Guid SeatType { get; set; }
            public int Quantity { get; set; }
        }

        [Serializable]
        public class UserDeactivated : ICorrelatedEvent
        {
            // Doesn't feel like this should be here? Part of the envelope?
            public Guid CorrelationId { get; set; }
        }

        public interface ICorrelatedEvent : IEvent
        {
            Guid CorrelationId { get; }
        }

        public class DelegateEventHandler<T> : IEventHandler<T>
            where T : class, IEvent
        {
            private Action<T> handler;

            public DelegateEventHandler(Action<T> handler)
            {
                this.handler = handler;
            }

            public void Handle(T @event)
            {
                this.handler(@event);
            }
        }

    }
}

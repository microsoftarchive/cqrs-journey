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

namespace WorkerRoleCommandProcessor
{
    using System.Linq;
    using System.Runtime.Caching;
    using System.Threading;
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.BlobStorage;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.Instrumentation;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.BlobStorage;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.Unity;
    using Microsoft.WindowsAzure;
    using Registration;
    using Registration.Handlers;

    /// <summary>
    /// Windows Azure side of the processor, which is included for compilation conditionally 
    /// at the csproj level.
    /// </summary>
    /// <devdoc>
    /// NOTE: this file is only compiled on non-DebugLocal configurations. In DebugLocal 
    /// you will not see full syntax coloring, IntelliSense, etc.. But it is still 
    /// much more readable and usable than a grayed-out piece of code inside an #if
    /// </devdoc>
    partial class ConferenceProcessor
    {
        private InfrastructureSettings azureSettings;
        private ServiceBusConfig busConfig;

        partial void OnCreating()
        {
            this.azureSettings = InfrastructureSettings.Read("Settings.xml");
            this.busConfig = new ServiceBusConfig(this.azureSettings.ServiceBus);

            busConfig.Initialize();
        }

        partial void OnCreateContainer(UnityContainer container)
        {
            var metadata = container.Resolve<IMetadataProvider>();
            var serializer = container.Resolve<ITextSerializer>();

            // blob
            var blobStorageAccount = CloudStorageAccount.Parse(azureSettings.BlobStorage.ConnectionString);
            container.RegisterInstance<IBlobStorage>(new CloudBlobStorage(blobStorageAccount, azureSettings.BlobStorage.RootContainerName));

            var commandBus = new CommandBus(new TopicSender(azureSettings.ServiceBus, Topics.Commands.Path), metadata, serializer);
            var eventsTopicSender = new TopicSender(azureSettings.ServiceBus, Topics.Events.Path);
            container.RegisterInstance<IMessageSender>("events", eventsTopicSender);
            container.RegisterInstance<IMessageSender>("orders", new TopicSender(azureSettings.ServiceBus, Topics.EventsOrders.Path));
            container.RegisterInstance<IMessageSender>("seatsavailability", new TopicSender(azureSettings.ServiceBus, Topics.EventsAvailability.Path));
            var eventBus = new EventBus(eventsTopicSender, metadata, serializer);

            var sessionlessCommandProcessor =
                new CommandProcessor(new SubscriptionReceiver(azureSettings.ServiceBus, Topics.Commands.Path, Topics.Commands.Subscriptions.Sessionless, false, new SubscriptionReceiverInstrumentation(Topics.Commands.Subscriptions.Sessionless, this.instrumentationEnabled)), serializer);
            var seatsAvailabilityCommandProcessor =
                new CommandProcessor(new SessionSubscriptionReceiver(azureSettings.ServiceBus, Topics.Commands.Path, Topics.Commands.Subscriptions.Seatsavailability, false, new SessionSubscriptionReceiverInstrumentation(Topics.Commands.Subscriptions.Seatsavailability, this.instrumentationEnabled)), serializer);

            var synchronousCommandBus = new SynchronousCommandBusDecorator(commandBus);
            container.RegisterInstance<ICommandBus>(synchronousCommandBus);

            container.RegisterInstance<IEventBus>(eventBus);
            container.RegisterInstance<IProcessor>("SessionlessCommandProcessor", sessionlessCommandProcessor);
            container.RegisterInstance<IProcessor>("SeatsAvailabilityCommandProcessor", seatsAvailabilityCommandProcessor);

            RegisterRepositories(container);
            RegisterEventProcessors(container);
            RegisterCommandHandlers(container, sessionlessCommandProcessor, seatsAvailabilityCommandProcessor);

            // handle order commands inline, as they do not have competition.
            synchronousCommandBus.Register(container.Resolve<ICommandHandler>("OrderCommandHandler"));

            // message log
            var messageLogAccount = CloudStorageAccount.Parse(azureSettings.MessageLog.ConnectionString);

            container.RegisterInstance<IProcessor>("EventLogger", new AzureMessageLogListener(
                new AzureMessageLogWriter(messageLogAccount, azureSettings.MessageLog.TableName),
                new SubscriptionReceiver(azureSettings.ServiceBus, Topics.Events.Path, Topics.Events.Subscriptions.Log)));

            container.RegisterInstance<IProcessor>("OrderEventLogger", new AzureMessageLogListener(
                new AzureMessageLogWriter(messageLogAccount, azureSettings.MessageLog.TableName),
                new SubscriptionReceiver(azureSettings.ServiceBus, Topics.EventsOrders.Path, Topics.EventsOrders.Subscriptions.LogOrders)));

            container.RegisterInstance<IProcessor>("SeatsAvailabilityEventLogger", new AzureMessageLogListener(
                new AzureMessageLogWriter(messageLogAccount, azureSettings.MessageLog.TableName),
                new SubscriptionReceiver(azureSettings.ServiceBus, Topics.EventsAvailability.Path, Topics.EventsAvailability.Subscriptions.LogAvail)));

            container.RegisterInstance<IProcessor>("CommandLogger", new AzureMessageLogListener(
                new AzureMessageLogWriter(messageLogAccount, azureSettings.MessageLog.TableName),
                new SubscriptionReceiver(azureSettings.ServiceBus, Topics.Commands.Path, Topics.Commands.Subscriptions.Log)));
        }

        private void RegisterEventProcessors(UnityContainer container)
        {
            container.RegisterType<RegistrationProcessManagerRouter>(new ContainerControlledLifetimeManager());

            container.RegisterEventProcessor<RegistrationProcessManagerRouter>(this.busConfig, Topics.Events.Subscriptions.RegistrationPMNextSteps, this.instrumentationEnabled);
            container.RegisterEventProcessor<PricedOrderViewModelGenerator>(this.busConfig, Topics.Events.Subscriptions.PricedOrderViewModelGeneratorV3, this.instrumentationEnabled);
            container.RegisterEventProcessor<ConferenceViewModelGenerator>(this.busConfig, Topics.Events.Subscriptions.ConferenceViewModelGenerator, this.instrumentationEnabled);

            container.RegisterEventProcessor<RegistrationProcessManagerRouter>(this.busConfig, Topics.EventsOrders.Subscriptions.RegistrationPMOrderPlacedOrders, this.instrumentationEnabled);
            container.RegisterEventProcessor<RegistrationProcessManagerRouter>(this.busConfig, Topics.EventsOrders.Subscriptions.RegistrationPMNextStepsOrders, this.instrumentationEnabled);
            container.RegisterEventProcessor<DraftOrderViewModelGenerator>(this.busConfig, Topics.EventsOrders.Subscriptions.OrderViewModelGeneratorOrders, this.instrumentationEnabled);
            container.RegisterEventProcessor<PricedOrderViewModelGenerator>(this.busConfig, Topics.EventsOrders.Subscriptions.PricedOrderViewModelOrders, this.instrumentationEnabled);
            container.RegisterEventProcessor<SeatAssignmentsViewModelGenerator>(this.busConfig, Topics.EventsOrders.Subscriptions.SeatAssignmentsViewModelOrders, this.instrumentationEnabled);
            container.RegisterEventProcessor<SeatAssignmentsHandler>(this.busConfig, Topics.EventsOrders.Subscriptions.SeatAssignmentsHandlerOrders, this.instrumentationEnabled);
            container.RegisterEventProcessor<Conference.OrderEventHandler>(this.busConfig, Topics.EventsOrders.Subscriptions.OrderEventHandlerOrders, this.instrumentationEnabled);

            container.RegisterEventProcessor<RegistrationProcessManagerRouter>(this.busConfig, Topics.EventsAvailability.Subscriptions.RegistrationPMNextStepsAvail, this.instrumentationEnabled);
            container.RegisterEventProcessor<ConferenceViewModelGenerator>(this.busConfig, Topics.EventsAvailability.Subscriptions.ConferenceViewModelAvail, this.instrumentationEnabled);
        }

        private static void RegisterCommandHandlers(IUnityContainer unityContainer, ICommandHandlerRegistry sessionlessRegistry, ICommandHandlerRegistry seatsAvailabilityRegistry)
        {
            var commandHandlers = unityContainer.ResolveAll<ICommandHandler>().ToList();
            var seatsAvailabilityHandler = commandHandlers.First(x => x.GetType().IsAssignableFrom(typeof(SeatsAvailabilityHandler)));

            seatsAvailabilityRegistry.Register(seatsAvailabilityHandler);
            foreach (var commandHandler in commandHandlers.Where(x => x != seatsAvailabilityHandler))
            {
                sessionlessRegistry.Register(commandHandler);
            }
        }

        private void RegisterRepositories(UnityContainer container)
        {
            // repository
            var eventSourcingAccount = CloudStorageAccount.Parse(this.azureSettings.EventSourcing.ConnectionString);
            var ordersEventStore = new EventStore(eventSourcingAccount, this.azureSettings.EventSourcing.OrdersTableName);
            var seatsAvailabilityEventStore = new EventStore(eventSourcingAccount, this.azureSettings.EventSourcing.SeatsAvailabilityTableName);

            container.RegisterInstance<IEventStore>("orders", ordersEventStore);
            container.RegisterInstance<IPendingEventsQueue>("orders", ordersEventStore);

            container.RegisterInstance<IEventStore>("seatsavailability", seatsAvailabilityEventStore);
            container.RegisterInstance<IPendingEventsQueue>("seatsavailability", seatsAvailabilityEventStore);

            container.RegisterType<IEventStoreBusPublisher, EventStoreBusPublisher>(
                "orders",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IMessageSender>("orders"),
                    new ResolvedParameter<IPendingEventsQueue>("orders"),
                    new EventStoreBusPublisherInstrumentation("worker - orders", this.instrumentationEnabled)));
            container.RegisterType<IEventStoreBusPublisher, EventStoreBusPublisher>(
                "seatsavailability",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IMessageSender>("seatsavailability"),
                    new ResolvedParameter<IPendingEventsQueue>("seatsavailability"),
                    new EventStoreBusPublisherInstrumentation("worker - seatsavailability", this.instrumentationEnabled)));

            var cache = new MemoryCache("RepositoryCache");

            container.RegisterType<IEventSourcedRepository<Order>, AzureEventSourcedRepository<Order>>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEventStore>("orders"),
                    new ResolvedParameter<IEventStoreBusPublisher>("orders"),
                    typeof(ITextSerializer),
                    typeof(IMetadataProvider),
                    cache));

            container.RegisterType<IEventSourcedRepository<SeatAssignments>, AzureEventSourcedRepository<SeatAssignments>>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEventStore>("orders"),
                    new ResolvedParameter<IEventStoreBusPublisher>("orders"),
                    typeof(ITextSerializer),
                    typeof(IMetadataProvider),
                    cache));

            container.RegisterType<IEventSourcedRepository<SeatsAvailability>, AzureEventSourcedRepository<SeatsAvailability>>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(
                    new ResolvedParameter<IEventStore>("seatsavailability"),
                    new ResolvedParameter<IEventStoreBusPublisher>("seatsavailability"),
                    typeof(ITextSerializer),
                    typeof(IMetadataProvider),
                    cache));

            // to satisfy the IProcessor requirements.
            container.RegisterInstance<IProcessor>(
                "OrdersEventStoreBusPublisher",
                new PublisherProcessorAdapter(container.Resolve<IEventStoreBusPublisher>("orders"), this.cancellationTokenSource.Token));
            container.RegisterInstance<IProcessor>(
                "SeatsAvailabilityEventStoreBusPublisher",
                new PublisherProcessorAdapter(container.Resolve<IEventStoreBusPublisher>("seatsavailability"), this.cancellationTokenSource.Token));
        }

        // to satisfy the IProcessor requirements.
        // TODO: we should unify and probably use token-based Start only processors.
        private class PublisherProcessorAdapter : IProcessor
        {
            private IEventStoreBusPublisher publisher;
            private CancellationToken token;

            public PublisherProcessorAdapter(IEventStoreBusPublisher publisher, CancellationToken token)
            {
                this.publisher = publisher;
                this.token = token;
            }

            public void Start()
            {
                this.publisher.Start(this.token);
            }

            public void Stop()
            {
                // Do nothing. The cancelled token will stop the process anyway.
            }
        }
    }
}

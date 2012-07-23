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

namespace MigrationToV3.InHouseProcessor
{
    using System.Linq;
    using System.Runtime.Caching;
    using System.Threading;
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.Instrumentation;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.BlobStorage;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.BlobStorage;
    using Microsoft.Practices.Unity;
    using Microsoft.WindowsAzure;
    using Registration;
    using WorkerRoleCommandProcessor;

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
            this.azureSettings.ServiceBus.Topics.First(t => t.IsEventBus).Subscriptions.AddRange(
                new[] 
                {
                    new SubscriptionSettings { Name = "Registration.RegistrationProcessRouter", RequiresSession = true },
                    new SubscriptionSettings { Name = "Registration.OrderViewModelGenerator", RequiresSession = true },
                    new SubscriptionSettings { Name = "Registration.PricedOrderViewModelGenerator", RequiresSession = true },
                    new SubscriptionSettings { Name = "Registration.SeatAssignmentsViewModelGenerator", RequiresSession = true },
                });
            this.azureSettings.ServiceBus.Topics.First(t => !t.IsEventBus).Subscriptions.AddRange(
                new[] 
                {
                    new SubscriptionSettings { Name = "all", RequiresSession = false}
                });

            this.busConfig = new ServiceBusConfig(this.azureSettings.ServiceBus);

            busConfig.Initialize();
        }

        partial void OnCreateContainer(UnityContainer container)
        {
            var metadata = container.Resolve<IMetadataProvider>();
            var serializer = container.Resolve<ITextSerializer>();

            // blob
            var blobStorageAccount = CloudStorageAccount.Parse(azureSettings.BlobStorage.ConnectionString);
            container.RegisterInstance<IBlobStorage>(new SqlBlobStorage("BlobStorage"));

            var commandBus = new CommandBus(new TopicSender(azureSettings.ServiceBus, Topics.Commands.Path), metadata, serializer);
            var topicSender = new TopicSender(azureSettings.ServiceBus, Topics.Events.Path);
            container.RegisterInstance<IMessageSender>(topicSender);
            var eventBus = new EventBus(topicSender, metadata, serializer);

            var commandProcessor =
                new CommandProcessor(new SubscriptionReceiver(azureSettings.ServiceBus, Topics.Commands.Path, "all", false, new SubscriptionReceiverInstrumentation("all", this.instrumentationEnabled)), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);

            container.RegisterInstance<IEventBus>(eventBus);
            container.RegisterInstance<IProcessor>("CommandProcessor", commandProcessor);

            RegisterRepository(container);
            RegisterEventProcessors(container);
            RegisterCommandHandlers(container, commandProcessor);
        }

        private void RegisterEventProcessors(UnityContainer container)
        {
            container.RegisterEventProcessor<RegistrationProcessManagerRouter>(this.busConfig, "Registration.RegistrationProcessRouter", this.instrumentationEnabled);
            container.RegisterEventProcessor<RegistrationV2.Handlers.OrderViewModelGenerator>(this.busConfig, "Registration.OrderViewModelGenerator", this.instrumentationEnabled);
            container.RegisterEventProcessor<RegistrationV2.Handlers.PricedOrderViewModelGenerator>(this.busConfig, "Registration.PricedOrderViewModelGenerator", this.instrumentationEnabled);
            container.RegisterEventProcessor<Registration.Handlers.SeatAssignmentsViewModelGenerator>(this.busConfig, "Registration.SeatAssignmentsViewModelGenerator", this.instrumentationEnabled);
        }

        private static void RegisterCommandHandlers(IUnityContainer unityContainer, ICommandHandlerRegistry registry)
        {
            var commandHandlers = unityContainer.ResolveAll<ICommandHandler>().ToList();
            foreach (var commandHandler in commandHandlers)
            {
                registry.Register(commandHandler);
            }
        }

        private void RegisterRepository(UnityContainer container)
        {
            // repository
            var eventSourcingAccount = CloudStorageAccount.Parse(this.azureSettings.EventSourcing.ConnectionString);
            var eventStore = new EventStore(eventSourcingAccount, this.azureSettings.EventSourcing.TableName);

            container.RegisterInstance<IEventStore>(eventStore);
            container.RegisterInstance<IPendingEventsQueue>(eventStore);
            container.RegisterInstance<IEventStoreBusPublisherInstrumentation>(new EventStoreBusPublisherInstrumentation("v3migration", this.instrumentationEnabled));
            container.RegisterType<IEventStoreBusPublisher, EventStoreBusPublisher>(new ContainerControlledLifetimeManager());
            var cache = new MemoryCache("RepositoryCache");
            container.RegisterType(
                typeof(IEventSourcedRepository<>),
                typeof(AzureEventSourcedRepository<>),
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(typeof(IEventStore), typeof(IEventStoreBusPublisher), typeof(ITextSerializer), typeof(IMetadataProvider), cache));

            // to satisfy the IProcessor requirements.
            container.RegisterInstance<IProcessor>("EventStoreBusPublisher", new PublisherProcessorAdapter(
                container.Resolve<IEventStoreBusPublisher>(), this.cancellationTokenSource.Token));
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

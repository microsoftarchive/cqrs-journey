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

namespace WorkerRoleCommandProcessor
{
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.Unity;
    using Microsoft.WindowsAzure;

    /// <summary>
    /// Azure-side of the processor, which is included for compilation conditionally 
    /// at the csproj level.
    /// </summary>
    /// <devdoc>
    /// NOTE: this file is only compiled on non-DebugLocal configurations. In DebugLocal 
    /// you will not see full syntax coloring, intellisense, etc.. But it is still 
    /// much more readable and usable than a grayed-out piece of code inside an #if
    /// </devdoc>
    partial class ConferenceProcessor
    {
        private InfrastructureSettings azureSettings;
        private ServiceBusConfig busConfig;

        partial void OnStart()
        {
            busConfig.Initialize();

            this.container.Resolve<IEventStoreBusPublisher>().Start(cancellationTokenSource.Token);
            this.container.Resolve<AzureMessageLogListener>("events").Start();
            this.container.Resolve<AzureMessageLogListener>("commands").Start();
            this.container.Resolve<CommandProcessor>().Start();
            this.container.Resolve<EventProcessor>().Start();
        }

        partial void OnStop()
        {
            this.container.Resolve<CommandProcessor>().Stop();
            this.container.Resolve<EventProcessor>().Stop();
            this.container.Resolve<AzureMessageLogListener>("events").Stop();
            this.container.Resolve<AzureMessageLogListener>("commands").Stop();
        }

        partial void OnCreateContainer(UnityContainer container)
        {
            RegisterInfrastructure(container);
            RegisterRepository(container);
        }

        private void RegisterInfrastructure(UnityContainer container)
        {
            var metadata = container.Resolve<IMetadataProvider>();
            var serializer = container.Resolve<ITextSerializer>();

            this.azureSettings = InfrastructureSettings.Read("Settings.xml");
            this.busConfig = new ServiceBusConfig(this.azureSettings.ServiceBus);

            var commandBus = new CommandBus(new TopicSender(azureSettings.ServiceBus, "conference/commands"), metadata, serializer);
            var topicSender = new TopicSender(azureSettings.ServiceBus, "conference/events");
            container.RegisterInstance<IMessageSender>(topicSender);
            var eventBus = new EventBus(topicSender, metadata, serializer);

            var commandProcessor = new CommandProcessor(new SubscriptionReceiver(azureSettings.ServiceBus, "conference/commands", "all"), serializer);
            var eventProcessor = new EventProcessor(new SessionSubscriptionReceiver(azureSettings.ServiceBus, "conference/events", "all"), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);
            container.RegisterInstance<IEventBus>(eventBus);
            container.RegisterInstance<ICommandHandlerRegistry>(commandProcessor);
            container.RegisterInstance(commandProcessor);
            container.RegisterInstance<IEventHandlerRegistry>(eventProcessor);
            container.RegisterInstance(eventProcessor);

            // message log
            var messageLogAccount = CloudStorageAccount.Parse(azureSettings.MessageLog.ConnectionString);

            container.RegisterInstance<AzureMessageLogListener>("events", new AzureMessageLogListener(
                new AzureMessageLogWriter(messageLogAccount, azureSettings.MessageLog.TableName),
                new SubscriptionReceiver(azureSettings.ServiceBus, "conference/events", "log")));

            container.RegisterInstance<AzureMessageLogListener>("commands", new AzureMessageLogListener(
                new AzureMessageLogWriter(messageLogAccount, azureSettings.MessageLog.TableName),
                new SubscriptionReceiver(azureSettings.ServiceBus, "conference/commands", "log")));
        }

        private void RegisterRepository(UnityContainer container)
        {
            // repository
            var eventSourcingAccount = CloudStorageAccount.Parse(this.azureSettings.EventSourcing.ConnectionString);
            var eventStore = new EventStore(eventSourcingAccount, this.azureSettings.EventSourcing.TableName);

            container.RegisterInstance<IEventStore>(eventStore);
            container.RegisterInstance<IPendingEventsQueue>(eventStore);
            container.RegisterType<IEventStoreBusPublisher, EventStoreBusPublisher>(new ContainerControlledLifetimeManager());
            container.RegisterType(typeof(IEventSourcedRepository<>), typeof(AzureEventSourcedRepository<>), new ContainerControlledLifetimeManager());
        }
    }
}

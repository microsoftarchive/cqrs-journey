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

namespace ConsoleCommandProcessor
{
    using System;
    using System.Data.Entity;
    using Azure;
    using Azure.Messaging;
    using Common;
    using Common.Sql;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json;
    using Registration;
    using Registration.Database;
    using Registration.Handlers;
    using Registration.ReadModel.Implementation;

    class Program
    {
        static void Main(string[] args)
        {
            using (var container = CreateContainer())
            {
                RegisterHandlers(container);

                Database.SetInitializer(new ConferenceRegistrationDbContextInitializer(new DropCreateDatabaseIfModelChanges<ConferenceRegistrationDbContext>()));
                Database.SetInitializer(new RegistrationProcessDbContextInitializer(new DropCreateDatabaseIfModelChanges<RegistrationProcessDbContext>()));
                Database.SetInitializer(new DropCreateDatabaseIfModelChanges<EventStoreDbContext>());

                using (var context = container.Resolve<ConferenceRegistrationDbContext>())
                {
                    context.Database.Initialize(true);
                }

                using (var context = container.Resolve<DbContext>("registration"))
                {
                    context.Database.Initialize(true);
                }

                using (var context = container.Resolve<EventStoreDbContext>())
                {
                    context.Database.Initialize(true);
                }

                container.Resolve<FakeSeatsAvailabilityInitializer>().Initialize();

                container.Resolve<CommandProcessor>().Start();
                container.Resolve<EventProcessor>().Start();

                Console.WriteLine("Host started");
                Console.WriteLine("Press enter to finish");
                Console.ReadLine();
            }
        }

        private static UnityContainer CreateContainer()
        {
            var container = new UnityContainer();
            // infrastructure
            var serializer = new JsonSerializerAdapter(JsonSerializer.Create(new JsonSerializerSettings
            {
                // Allows deserializing to the actual runtime type
                TypeNameHandling = TypeNameHandling.Objects,
                // In a version resilient way
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            }));
            container.RegisterInstance<ISerializer>(serializer);

            var settings = MessagingSettings.Read("Settings.xml");
            var commandBus = new CommandBus(new TopicSender(settings, "conference/commands"), new MetadataProvider(), serializer);
            var eventBus = new EventBus(new TopicSender(settings, "conference/events"), new MetadataProvider(), serializer);

            var commandProcessor = new CommandProcessor(new SubscriptionReceiver(settings, "conference/commands", "all"), serializer);
            var eventProcessor = new EventProcessor(new SubscriptionReceiver(settings, "conference/events", "all"), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);
            container.RegisterInstance<IEventBus>(eventBus);
            container.RegisterInstance<ICommandHandlerRegistry>(commandProcessor);
            container.RegisterInstance(commandProcessor);
            container.RegisterInstance<IEventHandlerRegistry>(eventProcessor);
            container.RegisterInstance(eventProcessor);


            // repository

            container.RegisterType<EventStoreDbContext>(new TransientLifetimeManager(), new InjectionConstructor("EventStore"));
            container.RegisterType(typeof(IEventSourcedRepository<>), typeof(SqlEventSourcedRepository<>), new ContainerControlledLifetimeManager());
            container.RegisterType<DbContext, RegistrationProcessDbContext>("registration", new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistrationProcesses"));
            container.RegisterType<IProcessDataContext<RegistrationProcess>, SqlProcessDataContext<RegistrationProcess>>(
                new TransientLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("registration"), typeof(ICommandBus)));

            container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistration"));


            // handlers

            container.RegisterType<IEventHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());

            container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
            container.RegisterType<ICommandHandler, SeatsAvailabilityHandler>("SeatsAvailabilityHandler");

            container.RegisterType<IEventHandler, OrderViewModelGenerator>("OrderViewModelGenerator");
            container.RegisterType<IEventHandler, ConferenceViewModelGenerator>("ConferenceViewModelGenerator");

            return container;
        }

        private static void RegisterHandlers(IUnityContainer unityContainer)
        {
            var commandHandlerRegistry = unityContainer.Resolve<ICommandHandlerRegistry>();
            var eventHandlerRegistry = unityContainer.Resolve<IEventHandlerRegistry>();

            foreach (var commandHandler in unityContainer.ResolveAll<ICommandHandler>())
            {
                commandHandlerRegistry.Register(commandHandler);
            }

            foreach (var eventHandler in unityContainer.ResolveAll<IEventHandler>())
            {
                eventHandlerRegistry.Register(eventHandler);
            }
        }
    }
}
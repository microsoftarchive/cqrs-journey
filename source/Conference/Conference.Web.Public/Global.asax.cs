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

namespace Conference.Web.Public
{
    using System;
    using System.Data.Entity;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Common;
    using Common.Sql;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json;
    using Payments;
    using Payments.Handlers;
    using Payments.ReadModel;
    using Payments.ReadModel.Implementation;
    using Registration;
    using Registration.Database;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class MvcApplication : System.Web.HttpApplication
    {
        private IUnityContainer container;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        protected void Application_Start()
        {
            this.container = CreateContainer();
            RegisterHandlers(this.container);

            DependencyResolver.SetResolver(new UnityServiceLocator(this.container));

            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AreaRegistration.RegisterAllAreas();
            AppRoutes.RegisterRoutes(RouteTable.Routes);

            Database.SetInitializer(new ConferenceRegistrationDbContextInitializer(new DropCreateDatabaseIfModelChanges<ConferenceRegistrationDbContext>()));
            Database.SetInitializer(new RegistrationProcessDbContextInitializer(new DropCreateDatabaseIfModelChanges<RegistrationProcessDbContext>()));
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<EventStoreDbContext>());

            using (var context = this.container.Resolve<ConferenceRegistrationDbContext>())
            {
                context.Database.Initialize(true);
            }

            Database.SetInitializer(new PaymentsDbContextInitializer(new Payments.Database.OrmRepositoryInitializer(new DropCreateDatabaseIfModelChanges<Payments.Database.OrmRepository>())));

            // Views repository is currently the same as the domain DB. No initializer needed.
            Database.SetInitializer<PaymentsDbContext>(null);

            using (var context = this.container.Resolve<DbContext>("registration"))
            {
                context.Database.Initialize(true);
            }

            using (var context = this.container.Resolve<EventStoreDbContext>())
            {
                context.Database.Initialize(true);
            }

            using (var context = this.container.Resolve<Payments.Database.OrmRepository>())
            {
                context.Database.Initialize(true);
            }

            container.Resolve<FakeSeatsAvailabilityInitializer>().Initialize();

#if !LOCAL
            this.container.Resolve<CommandProcessor>().Start();
            this.container.Resolve<EventProcessor>().Start();
#endif
        }

        protected void Application_Stop()
        {
            this.container.Dispose();
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

#if LOCAL
            container.RegisterType<ICommandBus, MemoryCommandBus>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandlerRegistry, MemoryCommandBus>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new MemoryCommandBus()));
            container.RegisterType<IEventBus, MemoryEventBus>(new ContainerControlledLifetimeManager());
            container.RegisterType<IEventHandlerRegistry, MemoryEventBus>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new MemoryEventBus()));
#else
            var settings = MessagingSettings.Read(HttpContext.Current.Server.MapPath("bin\\Settings.xml"));
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
#endif


            // repository

            container.RegisterType<EventStoreDbContext>(new TransientLifetimeManager(), new InjectionConstructor("EventStore"));
            container.RegisterType(typeof(IRepository<>), typeof(SqlEventRepository<>), new ContainerControlledLifetimeManager());
            container.RegisterType<DbContext, RegistrationProcessDbContext>("registration", new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistrationProcesses"));
            container.RegisterType<IProcessRepositorySession<RegistrationProcess>, SqlProcessRepositorySession<RegistrationProcess>>(
                new TransientLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter(typeof(Func<DbContext>), "registration"), typeof(ICommandBus)));
            container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistration"));

            container.RegisterType<IOrderDao, OrderDao>();
            container.RegisterType<IConferenceDao, ConferenceDao>();

            container.RegisterType<IRepository<ThirdPartyProcessorPayment>, Payments.Database.OrmRepository>(new InjectionConstructor(typeof(IEventBus)));

            container.RegisterType<PaymentsDbContext>(new TransientLifetimeManager(), new InjectionConstructor("Payments"));
            container.RegisterType<IPaymentDao, PaymentDao>();


            // handlers

            container.RegisterType<IEventHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());

            container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
            container.RegisterType<ICommandHandler, SeatsAvailabilityHandler>("SeatsAvailabilityHandler");
            container.RegisterType<IEventHandler, OrderViewModelGenerator>("OrderViewModelGenerator");

            container.RegisterType<ICommandHandler, ThirdPartyProcessorPaymentCommandHandler>(
                "ThirdPartyProcessorPaymentCommandHandler");

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

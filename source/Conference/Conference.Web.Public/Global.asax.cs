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
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Azure;
    using Azure.Messaging;
    using Common;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json;
    using Registration;
    using Registration.Database;
    using Registration.Handlers;
    using Registration.ReadModel;

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
            AppRoutes.RegisterRoutes(RouteTable.Routes);

            Database.SetInitializer(new OrmViewRepositoryInitializer(new OrmRepositoryInitializer(new DropCreateDatabaseIfModelChanges<OrmRepository>())));
            Database.SetInitializer(new OrmSagaRepositoryInitializer(new DropCreateDatabaseIfModelChanges<OrmSagaRepository>()));

            // Views repository is currently the same as the domain DB. No initializer needed.
            Database.SetInitializer<OrmViewRepository>(null);

            using (var context = this.container.Resolve<OrmRepository>("registration"))
            {
                context.Database.Initialize(true);
            }

            using (var context = this.container.Resolve<OrmSagaRepository>("registration"))
            {
                context.Database.Initialize(true);
            }

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
#if LOCAL
            container.RegisterType<ICommandBus, MemoryCommandBus>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandlerRegistry, MemoryCommandBus>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new MemoryCommandBus()));
            container.RegisterType<IEventBus, MemoryEventBus>(new ContainerControlledLifetimeManager());
            container.RegisterType<IEventHandlerRegistry, MemoryEventBus>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new MemoryEventBus()));
#else
            var serializer = new JsonSerializerAdapter(JsonSerializer.Create(new JsonSerializerSettings
            {
                // Allows deserializing to the actual runtime type
                TypeNameHandling = TypeNameHandling.Objects,
                // In a version resilient way
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            }));

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

            container.RegisterType<IRepository, Registration.Database.OrmRepository>("registration", new InjectionConstructor(typeof(IEventBus)));
            container.RegisterType<ISagaRepository, Registration.Database.OrmSagaRepository>("registration", new InjectionConstructor(typeof(ICommandBus)));
            container.RegisterType<IViewRepository, Registration.ReadModel.OrmViewRepository>("registration", new InjectionConstructor());


            // handlers

            container.RegisterType<IEventHandler, RegistrationProcessSagaRouter>(
                "RegistrationProcessSagaRouter",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<ISagaRepository>>("registration")));
            container.RegisterType<ICommandHandler, RegistrationProcessSagaRouter>(
                "RegistrationProcessSagaRouter",
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<ISagaRepository>>("registration")));

            container.RegisterType<ICommandHandler, OrderCommandHandler>(
                "OrderCommandHandler",
                new InjectionConstructor(new ResolvedParameter<Func<IRepository>>("registration")));

            container.RegisterType<ICommandHandler, SeatsAvailabilityHandler>(
                "SeatsAvailabilityHandler",
                new InjectionConstructor(new ResolvedParameter<Func<IRepository>>("registration")));

            container.RegisterType<IEventHandler, OrderViewModelGenerator>(
                "OrderViewModelGenerator",
                new InjectionConstructor(new ResolvedParameter<Func<IViewRepository>>("registration")));

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

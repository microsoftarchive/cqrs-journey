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
    using Conference.Common.Entity;
    using Infrastructure.Azure;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Database;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Messaging.InMemory;
    using Infrastructure.Processes;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.Database;
    using Infrastructure.Sql.EventSourcing;
    using Infrastructure.Sql.Processes;
    using Microsoft.Practices.Unity;
    using Newtonsoft.Json;
    using Payments;
    using Payments.Database;
    using Payments.Handlers;
    using Payments.ReadModel;
    using Payments.ReadModel.Implementation;
    using Registration;
    using Registration.Database;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Infrastructure.Sql.Blob;

    public class MvcApplication : System.Web.HttpApplication
    {
        private IUnityContainer container;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        protected void Application_Start()
        {
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);

            this.container = CreateContainer();
#if LOCAL
            RegisterHandlers(this.container);
#endif

            DependencyResolver.SetResolver(new UnityServiceLocator(this.container));

            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AreaRegistration.RegisterAllAreas();
            AppRoutes.RegisterRoutes(RouteTable.Routes);

#if LOCAL
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
            Database.SetInitializer<RegistrationProcessDbContext>(null);
            Database.SetInitializer<EventStoreDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);

            Database.SetInitializer<PaymentsDbContext>(null);
            Database.SetInitializer<PaymentsReadDbContext>(null);
#else
            Database.SetInitializer<PaymentsReadDbContext>(null);
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
#endif

            if (Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.IsAvailable)
            {
                System.Diagnostics.Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());
                System.Diagnostics.Trace.AutoFlush = true;
            }
        }

        protected void Application_Stop()
        {
            this.container.Dispose();
        }

        private static UnityContainer CreateContainer()
        {
            var container = new UnityContainer();
            // infrastructure
            var serializer = new JsonTextSerializer();
            container.RegisterInstance<ITextSerializer>(serializer);

#if LOCAL
            container.RegisterType<ICommandBus, MemoryCommandBus>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandlerRegistry, MemoryCommandBus>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new MemoryCommandBus()));
            container.RegisterType<IEventBus, MemoryEventBus>(new ContainerControlledLifetimeManager());
            container.RegisterType<IEventHandlerRegistry, MemoryEventBus>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new MemoryEventBus()));
#else
            var settings = InfrastructureSettings.ReadMessaging(HttpContext.Current.Server.MapPath(@"~\bin\Settings.xml"));
            var commandBus = new CommandBus(new TopicSender(settings, "conference/commands"), new MetadataProvider(), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);
#endif


            // repository

#if LOCAL
            container.RegisterType<EventStoreDbContext>(new TransientLifetimeManager(), new InjectionConstructor("EventStore"));
            container.RegisterType<BlobStorageDbContext>(new TransientLifetimeManager(), new InjectionConstructor("BlobStorage"));
            container.RegisterType(typeof(IEventSourcedRepository<>), typeof(SqlEventSourcedRepository<>), new ContainerControlledLifetimeManager());
            container.RegisterType<DbContext, RegistrationProcessDbContext>("registration", new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistrationProcesses"));
            container.RegisterType<IProcessDataContext<RegistrationProcess>, SqlProcessDataContext<RegistrationProcess>>(
                new TransientLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("registration"), typeof(ICommandBus)));

            container.RegisterType<DbContext, PaymentsDbContext>("payments", new TransientLifetimeManager(), new InjectionConstructor("Payments"));
            container.RegisterType<IDataContext<ThirdPartyProcessorPayment>, SqlDataContext<ThirdPartyProcessorPayment>>(
                new TransientLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("payments"), typeof(IEventBus)));
#endif
            container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistration"));
            container.RegisterType<PaymentsReadDbContext>(new TransientLifetimeManager(), new InjectionConstructor("Payments"));

            container.RegisterType<IOrderDao, OrderDao>();
            container.RegisterType<IConferenceDao, ConferenceDao>();
            container.RegisterType<IPaymentDao, PaymentDao>();



#if LOCAL
            // handlers

            container.RegisterType<IEventHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());

            container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
            container.RegisterType<ICommandHandler, SeatsAvailabilityHandler>("SeatsAvailabilityHandler");

            container.RegisterType<ICommandHandler, ThirdPartyProcessorPaymentCommandHandler>("ThirdPartyProcessorPaymentCommandHandler");

            container.RegisterType<IEventHandler, OrderViewModelGenerator>("OrderViewModelGenerator");
            container.RegisterType<IEventHandler, ConferenceViewModelGenerator>("ConferenceViewModelGenerator");
#endif

            return container;
        }

#if LOCAL
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
#endif
    }
}

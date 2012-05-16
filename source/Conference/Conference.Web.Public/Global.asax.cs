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
    using System.Web;
    using System.Data.Entity;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Common.Entity;
    using Infrastructure;
    using Infrastructure.BlobStorage;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.BlobStorage;
    using Microsoft.Practices.Unity;
    using Payments.ReadModel;
    using Payments.ReadModel.Implementation;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
#if LOCAL
    using Infrastructure.Sql.Messaging;
    using Infrastructure.Sql.Messaging.Implementation;
#else
    using Infrastructure.Azure.Messaging;
    using Infrastructure;
#endif

    public class MvcApplication : HttpApplication
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

            DependencyResolver.SetResolver(new UnityServiceLocator(this.container));

            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AreaRegistration.RegisterAllAreas();
            AppRoutes.RegisterRoutes(RouteTable.Routes);

            Database.SetInitializer<BlobStorageDbContext>(null);
            Database.SetInitializer<PaymentsReadDbContext>(null);
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);

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
            container.RegisterType<IMessageSender, MessageSender>(
                "Commands", new TransientLifetimeManager(), new InjectionConstructor(Database.DefaultConnectionFactory, "SqlBus", "SqlBus.Commands"));
            container.RegisterType<ICommandBus, CommandBus>(
                new ContainerControlledLifetimeManager(), new InjectionConstructor(new ResolvedParameter<IMessageSender>("Commands"), serializer));
#else
            var settings = InfrastructureSettings.Read(HttpContext.Current.Server.MapPath(@"~\bin\Settings.xml")).Messaging;
            var commandBus = new CommandBus(new TopicSender(settings, "conference/commands"), new StandardMetadataProvider(), serializer);

            container.RegisterInstance<ICommandBus>(commandBus);
#endif

            // repository

            container.RegisterType<IBlobStorage, SqlBlobStorage>(new ContainerControlledLifetimeManager(), new InjectionConstructor("BlobStorage"));
            container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistration"));
            container.RegisterType<PaymentsReadDbContext>(new TransientLifetimeManager(), new InjectionConstructor("Payments"));

            container.RegisterType<IOrderDao, OrderDao>();
            container.RegisterType<IConferenceDao, ConferenceDao>();
            container.RegisterType<IPaymentDao, PaymentDao>();

            return container;
        }
    }
}

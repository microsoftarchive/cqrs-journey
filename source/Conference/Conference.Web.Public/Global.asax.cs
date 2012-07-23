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

namespace Conference.Web.Public
{
    using System.Runtime.Caching;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Common;
    using Conference.Web.Utils;
    using Microsoft.Practices.Unity;
    using Payments.ReadModel;
    using Payments.ReadModel.Implementation;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public partial class MvcApplication : HttpApplication
    {
        private IUnityContainer container;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new MaintenanceModeAttribute());
            filters.Add(new HandleErrorAttribute());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "By design")]
        protected void Application_Start()
        {
#if AZURESDK
            Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.Changed +=
                (s, a) =>
                {
                    Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.RequestRecycle();
                };
#endif
            MaintenanceMode.RefreshIsInMaintainanceMode();

            DatabaseSetup.Initialize();

            this.container = CreateContainer();

            DependencyResolver.SetResolver(new UnityServiceLocator(this.container));

            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AreaRegistration.RegisterAllAreas();
            AppRoutes.RegisterRoutes(RouteTable.Routes);

#if AZURESDK
            if (Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.IsAvailable)
            {
                System.Diagnostics.Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());
                System.Diagnostics.Trace.AutoFlush = true;
            }
#endif

            this.OnStart();
        }

        protected void Application_Stop()
        {
            this.OnStop();

            this.container.Dispose();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private static UnityContainer CreateContainer()
        {
            var container = new UnityContainer();
            try
            {
                // repositories used by the application

                container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistration"));
                container.RegisterType<PaymentsReadDbContext>(new TransientLifetimeManager(), new InjectionConstructor("Payments"));

                var cache = new MemoryCache("ReadModel");
                container.RegisterType<IOrderDao, OrderDao>();
                container.RegisterType<IConferenceDao, CachingConferenceDao>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionConstructor(new ResolvedParameter<ConferenceDao>(), cache));
                container.RegisterType<IPaymentDao, PaymentDao>();

                // configuration specific settings

                OnCreateContainer(container);

                return container;
            }
            catch
            {
                container.Dispose();
                throw;
            }
        }

        static partial void OnCreateContainer(UnityContainer container);

        partial void OnStart();

        partial void OnStop();
    }
}

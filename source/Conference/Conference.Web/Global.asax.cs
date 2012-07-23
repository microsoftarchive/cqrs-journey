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

namespace Conference.Web.Admin
{
    using System.Data.Entity;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Common;
    using Conference.Common.Entity;
    using Conference.Web.Utils;
    using Infrastructure;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
#if LOCAL
    using Infrastructure.Sql.Messaging;
    using Infrastructure.Sql.Messaging.Implementation;
#else
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure;
#endif

    public class MvcApplication : HttpApplication
    {
        public static IEventBus EventBus { get; private set; }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new MaintenanceModeAttribute());
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Conference.Locate",
                url: "locate",
                defaults: new { controller = "Conference", action = "Locate" }
            );

            routes.MapRoute(
                name: "Conference.Create",
                url: "create",
                defaults: new { controller = "Conference", action = "Create" }
            );

            routes.MapRoute(
                name: "Conference",
                url: "{slug}/{accessCode}/{action}",
                defaults: new { controller = "Conference", action = "Index" }
            );

            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new { controller = "Home", action = "Index" }
            );

        }

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

            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            var serializer = new JsonTextSerializer();
#if LOCAL
            EventBus = new EventBus(new MessageSender(Database.DefaultConnectionFactory, "SqlBus", "SqlBus.Events"), serializer);
#else
            var settings = InfrastructureSettings.Read(HttpContext.Current.Server.MapPath(@"~\bin\Settings.xml")).ServiceBus;

            if (!MaintenanceMode.IsInMaintainanceMode)
            {
            new ServiceBusConfig(settings).Initialize();
            }

            EventBus = new EventBus(new TopicSender(settings, "conference/events"), new StandardMetadataProvider(), serializer);
#endif

#if AZURESDK
            if (Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.IsAvailable)
            {
                System.Diagnostics.Trace.Listeners.Add(new Microsoft.WindowsAzure.Diagnostics.DiagnosticMonitorTraceListener());
                System.Diagnostics.Trace.AutoFlush = true;
            }
#endif
        }
    }
}

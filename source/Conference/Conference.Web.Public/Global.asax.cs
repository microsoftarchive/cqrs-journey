// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Conference.Web.Public
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Common;
    using Registration.Database;
    using Registration.ReadModel;

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        protected void Application_Start()
        {
            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AppRoutes.RegisterRoutes(RouteTable.Routes);

            Services = GetDefaultServices();

            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<OrmRepository>());
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<OrmSagaRepository>());
        }

        public static IDictionary<Type, object> GetDefaultServices()
        {
            var services = new Dictionary<Type, object>();

            services[typeof(ICommandBus)] = CreateCommandBus();
            services[typeof(IEventBus)] = CreateEventBus();

            services[typeof(IOrderReadModel)] = new OrmOrderReadModel(new OrmRepository());
            services[typeof(IConferenceReadModel)] = new ConferenceReadModel();


            return services;
        }

        private static MemoryCommandBus CreateCommandBus()
        {
            // TODO add handlers
            var handlers = new ICommandHandler[] { };

            return new MemoryCommandBus(handlers);
        }

        private static MemoryEventBus CreateEventBus()
        {
            // TODO add handlers
            var handlers = new IEventHandler[] { };

            return new MemoryEventBus(handlers);
        }

        private static IDictionary<Type, object> Services = new Dictionary<Type, object>();

        public static T GetService<T>()
            where T : class
        {
            object service;
            if (!Services.TryGetValue(typeof(T), out service))
            {
                return null;
            }

            return service as T;
        }
    }
}
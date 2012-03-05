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
    using System.Web.Mvc;
    using System.Web.Routing;
    using Common;
    using System.Data.Entity;
    using Registration.Database;

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

            RegisterServices(Services);

            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<OrmRepository>());
        }

        public static void RegisterServices(Dictionary<Type, object> services)
        {
            services.Clear();

            services[typeof(ICommandBus)] = null;
        }

        private static Dictionary<Type, object> Services = new Dictionary<Type, object>();

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
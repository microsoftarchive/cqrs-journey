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
    using Registration.Handlers;
    using Registration.ReadModel;

    public class MvcApplication : System.Web.HttpApplication
    {
        private static IDictionary<Type, object> services = new Dictionary<Type, object>();

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        protected void Application_Start()
        {
            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            AppRoutes.RegisterRoutes(RouteTable.Routes);

            services = GetDefaultServices();

            Database.SetInitializer(new OrmRepositoryInitializer(new DropCreateDatabaseIfModelChanges<OrmRepository>()));
            Database.SetInitializer(new OrmSagaRepositoryInitializer(new DropCreateDatabaseIfModelChanges<OrmSagaRepository>()));
        }

        public static IDictionary<Type, object> GetDefaultServices()
        {
            var services = new Dictionary<Type, object>();

            var commandBus = new MemoryCommandBus();
            var eventBus = new MemoryEventBus();

            // Handlers
            var registrationSaga = new RegistrationProcessSagaHandler(() => new OrmSagaRepository(commandBus));

            commandBus.Register(registrationSaga);
            eventBus.Register(registrationSaga);

            commandBus.Register(new RegistrationCommandHandler(() => new OrmRepository(eventBus)));

            /// This will be replaced with an AzureDelayCommandHandler that will 
            /// leverage azure service bus capabilities for sending delayed messages.
            commandBus.Register(new MemoryDelayCommandHandler(commandBus));


            services[typeof(ICommandBus)] = commandBus;
            services[typeof(IEventBus)] = eventBus;
            services[typeof(IOrderReadModel)] = new OrmOrderReadModel(() => new OrmRepository(eventBus));
            services[typeof(IConferenceReadModel)] = new ConferenceReadModel();

            return services;
        }

        public static T GetService<T>()
            where T : class
        {
            object service;
            if (!services.TryGetValue(typeof(T), out service))
            {
                return null;
            }

            return service as T;
        }
    }
}
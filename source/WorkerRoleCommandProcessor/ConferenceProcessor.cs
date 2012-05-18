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

namespace WorkerRoleCommandProcessor
{
    using System;
    using System.Data.Entity;
    using System.Threading;
    using Infrastructure;
    using Infrastructure.BlobStorage;
    using Infrastructure.Database;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Processes;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.BlobStorage;
    using Infrastructure.Sql.Database;
    using Infrastructure.Sql.EventSourcing;
    using Infrastructure.Sql.MessageLog;
    using Infrastructure.Sql.Processes;
    using Microsoft.Practices.Unity;
    using Payments;
    using Payments.Database;
    using Payments.Handlers;
    using Payments.ReadModel.Implementation;
    using Registration;
    using Registration.Database;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public sealed partial class ConferenceProcessor : IDisposable
    {
        private IUnityContainer container;
        private CancellationTokenSource cancellationTokenSource;

        public ConferenceProcessor()
        {
            this.container = CreateContainer();
            RegisterHandlers(container);

            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
            Database.SetInitializer<RegistrationProcessDbContext>(null);
            Database.SetInitializer<EventStoreDbContext>(null);
            Database.SetInitializer<MessageLogDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);

            Database.SetInitializer<PaymentsDbContext>(null);
            Database.SetInitializer<PaymentsReadDbContext>(null);
        }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            OnStart();
        }

        partial void OnStart();

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();

            OnStop();
        }

        partial void OnStop();

        public void Dispose()
        {
            this.container.Dispose();
        }

        partial void OnCreateContainer(UnityContainer container);

        private UnityContainer CreateContainer()
        {
            var container = new UnityContainer();

            // infrastructure
            container.RegisterInstance<ITextSerializer>(new JsonTextSerializer());
            container.RegisterInstance<IMetadataProvider>(new StandardMetadataProvider());

            OnCreateContainer(container);

            container.RegisterType<IBlobStorage, SqlBlobStorage>(new ContainerControlledLifetimeManager(), new InjectionConstructor("BlobStorage"));
            container.RegisterType<DbContext, RegistrationProcessDbContext>("registration", new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistrationProcesses"));
            container.RegisterType<IProcessDataContext<RegistrationProcess>, SqlProcessDataContext<RegistrationProcess>>(
                new TransientLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("registration"), typeof(ICommandBus)));

            container.RegisterType<DbContext, PaymentsDbContext>("payments", new TransientLifetimeManager(), new InjectionConstructor("Payments"));
            container.RegisterType<IDataContext<ThirdPartyProcessorPayment>, SqlDataContext<ThirdPartyProcessorPayment>>(
                new TransientLifetimeManager(),
                new InjectionConstructor(new ResolvedParameter<Func<DbContext>>("payments"), typeof(IEventBus)));

            container.RegisterType<ConferenceRegistrationDbContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceRegistration"));

            container.RegisterType<IConferenceDao, ConferenceDao>(new ContainerControlledLifetimeManager());
            container.RegisterType<IOrderDao, OrderDao>(new ContainerControlledLifetimeManager());
            // handlers

            container.RegisterType<IEventHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandler, RegistrationProcessRouter>("RegistrationProcessRouter", new ContainerControlledLifetimeManager());

            container.RegisterType<IPricingService, PricingService>(new ContainerControlledLifetimeManager());
            container.RegisterType<ICommandHandler, OrderCommandHandler>("OrderCommandHandler");
            container.RegisterType<ICommandHandler, SeatsAvailabilityHandler>("SeatsAvailabilityHandler");

            container.RegisterType<ICommandHandler, ThirdPartyProcessorPaymentCommandHandler>("ThirdPartyProcessorPaymentCommandHandler");

            container.RegisterType<IEventHandler, OrderViewModelGenerator>("OrderViewModelGenerator");
            container.RegisterType<IEventHandler, PricedOrderViewModelGenerator>("PricedOrderViewModelGenerator");
            container.RegisterType<IEventHandler, ConferenceViewModelGenerator>("ConferenceViewModelGenerator");
            container.RegisterType<IEventHandler, SeatAssignmentsViewModelGenerator>("SeatAssignmentsViewModelGenerator");

            container.RegisterType<ICommandHandler, SeatAssignmentsHandler>("SeatAssignmentsHandler");
            container.RegisterType<IEventHandler, SeatAssignmentsHandler>("SeatAssignmentsHandler");

            // Conference management integration
            container.RegisterType<global::Conference.ConferenceContext>(new TransientLifetimeManager(), new InjectionConstructor("ConferenceManagement"));
            container.RegisterType<IEventHandler, global::Conference.OrderEventHandler>("Conference.OrderEventHandler");

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

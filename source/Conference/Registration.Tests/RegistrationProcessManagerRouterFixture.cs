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

namespace Registration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.Messaging;
    using Infrastructure.Processes;
    using Payments.Contracts.Events;
    using Registration.Events;
    using Xunit;

    public class RegistrationProcessManagerRouterFixture
    {
        [Fact]
        public void when_order_placed_then_routes_and_saves()
        {
            var context = new StubProcessManagerDataContext<RegistrationProcessManager>();
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderPlaced { SourceId = Guid.NewGuid(), ConferenceId = Guid.NewGuid(), Seats = new SeatQuantity[0] });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_placed_is_is_reprocessed_then_routes_and_saves()
        {
            var pm = new RegistrationProcessManager
            {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                OrderId = Guid.NewGuid(),
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderPlaced { SourceId = pm.OrderId, ConferenceId = pm.ConferenceId, Seats = new SeatQuantity[0] });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_updated_then_routes_and_saves()
        {
            var pm = new RegistrationProcessManager
            {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderUpdated { SourceId = pm.OrderId, Seats = new SeatQuantity[0] });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_reservation_accepted_then_routes_and_saves()
        {
            var pm = new RegistrationProcessManager
            {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                SeatReservationCommandId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(
                new Envelope<SeatsReserved>(
                    new SeatsReserved { SourceId = pm.ConferenceId, ReservationId = pm.ReservationId, ReservationDetails = new SeatQuantity[0] })
                    {
                        CorrelationId = pm.SeatReservationCommandId.ToString()
                    });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_payment_received_then_routes_and_saves()
        {
            var pm = new RegistrationProcessManager
            {
                State = RegistrationProcessManager.ProcessState.ReservationConfirmationReceived,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new PaymentCompleted { PaymentSourceId = pm.OrderId });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_confirmed_received_then_routes_and_saves()
        {
            var pm = new RegistrationProcessManager
            {
                State = RegistrationProcessManager.ProcessState.PaymentConfirmationReceived,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };
            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new OrderConfirmed { SourceId = pm.OrderId });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_expired_then_routes_and_saves()
        {
            var pm = new RegistrationProcessManager
            {
                State = RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessManagerDataContext<RegistrationProcessManager> { Store = { pm } };

            var router = new RegistrationProcessManagerRouter(() => context);

            router.Handle(new Commands.ExpireRegistrationProcess { ProcessId = pm.Id });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }
    }

    class StubProcessManagerDataContext<T> : IProcessManagerDataContext<T> where T : class, IProcessManager
    {
        public readonly List<T> SavedProcesses = new List<T>();

        public readonly List<T> Store = new List<T>();

        public bool DisposeCalled { get; set; }

        public T Find(Guid id)
        {
            return this.Store.SingleOrDefault(x => x.Id == id);
        }

        public void Save(T processManager)
        {
            this.SavedProcesses.Add(processManager);
        }

        public T Find(Expression<Func<T, bool>> predicate, bool includeCompleted = false)
        {
            return this.Store.AsQueryable().Where(x => includeCompleted || !x.Completed).SingleOrDefault(predicate);
        }

        public void Dispose()
        {
            this.DisposeCalled = true;
        }
    }
}

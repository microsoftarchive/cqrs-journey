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

namespace Registration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Common;
    using Registration.Events;
    using Xunit;

    public class RegistrationProcessRouterFixture
    {
        [Fact]
        public void when_order_placed_then_routes_and_saves()
        {
            var repo = new StubProcessRepository();
            var router = new RegistrationProcessRouter(() => repo);

            router.Handle(new OrderPlaced(Guid.NewGuid(), -1, Guid.NewGuid(), new SeatQuantity[0], DateTime.UtcNow, null));

            Assert.Equal(1, repo.SavedProcesses.Cast<RegistrationProcess>().Count());
            Assert.True(repo.DisposeCalled);
        }

        [Fact]
        public void when_reservation_accepted_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var repo = new StubProcessRepository { Store = { process } };
            var router = new RegistrationProcessRouter(() => repo);

            router.Handle(new SeatsReserved(process.ConferenceId, -1, process.ReservationId, new SeatQuantity[0], new SeatQuantity[0]));

            Assert.Equal(1, repo.SavedProcesses.Cast<RegistrationProcess>().Count());
            Assert.True(repo.DisposeCalled);
        }

        [Fact]
        public void when_order_expired_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var repo = new StubProcessRepository { Store = { process } };

            var router = new RegistrationProcessRouter(() => repo);

            router.Handle(new Commands.ExpireRegistrationProcess { ProcessId = process.Id });

            Assert.Equal(1, repo.SavedProcesses.Count);
            Assert.True(repo.DisposeCalled);
        }

        [Fact]
        public void when_payment_received_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.AwaitingPayment,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var repo = new StubProcessRepository { Store = { process } };
            var router = new RegistrationProcessRouter(() => repo);

            router.Handle(new PaymentReceived { OrderId = process.OrderId });

            Assert.Equal(1, repo.SavedProcesses.Cast<RegistrationProcess>().Count());
            Assert.True(repo.DisposeCalled);
        }
    }

    class StubProcessRepository : IProcessRepository, IDisposable
    {
        public readonly List<IAggregateRoot> SavedProcesses = new List<IAggregateRoot>();

        public readonly List<IAggregateRoot> Store = new List<IAggregateRoot>();

        public bool DisposeCalled { get; set; }

        public T Find<T>(Guid id) where T : class, IAggregateRoot
        {
            return this.Store.OfType<T>().SingleOrDefault(x => x.Id == id);
        }

        public void Save<T>(T process) where T : class, IAggregateRoot
        {
            this.SavedProcesses.Add(process);
        }

        public T Find<T>(Expression<Func<T, bool>> predicate) where T : class, IAggregateRoot
        {
            return this.Store.OfType<T>().AsQueryable().SingleOrDefault(predicate);
        }

        public void Dispose()
        {
            this.DisposeCalled = true;
        }
    }
}

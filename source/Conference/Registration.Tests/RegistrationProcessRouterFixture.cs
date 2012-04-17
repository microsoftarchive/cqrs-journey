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
    using System.Linq;
    using Common;
    using Moq;
    using Registration.Events;
    using Xunit;

    public class RegistrationProcessRouterFixture
    {
        [Fact]
        public void when_order_placed_then_routes_and_saves()
        {
            var repo = new Mock<IProcessRepository>();
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessRouter(() => repo.Object);

            router.Handle(new OrderPlaced(Guid.NewGuid(), -1, Guid.NewGuid(), new SeatQuantity[0], DateTime.UtcNow, null));

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcess>()));
            disposable.Verify(x => x.Dispose());
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
            var repo = new Mock<IProcessRepository>();
            repo.Setup(x => x.Query<RegistrationProcess>()).Returns(new[] { process }.AsQueryable());
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessRouter(() => repo.Object);

            router.Handle(new SeatsReserved(process.ConferenceId, -1, process.ReservationId, new SeatQuantity[0], new SeatQuantity[0]));

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcess>()));
            disposable.Verify(x => x.Dispose());
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
            var repo = new Mock<IProcessRepository>();
            repo.Setup(x => x.Query<RegistrationProcess>()).Returns(new[] { process }.AsQueryable());
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessRouter(() => repo.Object);

            router.Handle(new Commands.ExpireRegistrationProcess { ProcessId = process.Id });

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcess>()));
            disposable.Verify(x => x.Dispose());
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
            var repo = new Mock<IProcessRepository>();
            repo.Setup(x => x.Query<RegistrationProcess>()).Returns(new[] { process }.AsQueryable());
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessRouter(() => repo.Object);

            router.Handle(new Events.PaymentReceived { OrderId = process.OrderId });

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcess>()));
            disposable.Verify(x => x.Dispose());
        }

    }
}

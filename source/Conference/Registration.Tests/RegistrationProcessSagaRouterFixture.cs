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

    public class RegistrationProcessSagaRouterFixture
    {
        [Fact]
        public void when_order_placed_then_routes_and_saves()
        {
            var repo = new Mock<ISagaRepository>();
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessSagaRouter(() => repo.Object);

            router.Handle(new OrderPlaced(Guid.NewGuid(), Guid.NewGuid(), new SeatQuantity[0], DateTime.UtcNow, null));

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcessSaga>()));
            disposable.Verify(x => x.Dispose());
        }

        [Fact]
        public void when_reservation_accepted_then_routes_and_saves()
        {
            var saga = new RegistrationProcessSaga
            {
                State = RegistrationProcessSaga.SagaState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var repo = new Mock<ISagaRepository>();
            repo.Setup(x => x.Query<RegistrationProcessSaga>()).Returns(new[] { saga }.AsQueryable());
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessSagaRouter(() => repo.Object);

            router.Handle(new SeatsReserved
            {
                ReservationId = saga.ReservationId,
            });

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcessSaga>()));
            disposable.Verify(x => x.Dispose());
        }

        [Fact]
        public void when_order_expired_then_routes_and_saves()
        {
            var saga = new RegistrationProcessSaga
            {
                State = RegistrationProcessSaga.SagaState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var repo = new Mock<ISagaRepository>();
            repo.Setup(x => x.Query<RegistrationProcessSaga>()).Returns(new[] { saga }.AsQueryable());
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessSagaRouter(() => repo.Object);

            router.Handle(new Commands.ExpireRegistrationProcess { ProcessId = saga.Id });

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcessSaga>()));
            disposable.Verify(x => x.Dispose());
        }

        [Fact]
        public void when_payment_received_then_routes_and_saves()
        {
            var saga = new RegistrationProcessSaga
            {
                State = RegistrationProcessSaga.SagaState.AwaitingPayment,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var repo = new Mock<ISagaRepository>();
            repo.Setup(x => x.Query<RegistrationProcessSaga>()).Returns(new[] { saga }.AsQueryable());
            var disposable = repo.As<IDisposable>();
            var router = new RegistrationProcessSagaRouter(() => repo.Object);

            router.Handle(new Events.PaymentReceived { OrderId = saga.OrderId });

            repo.Verify(x => x.Save(It.IsAny<RegistrationProcessSaga>()));
            disposable.Verify(x => x.Dispose());
        }

    }
}

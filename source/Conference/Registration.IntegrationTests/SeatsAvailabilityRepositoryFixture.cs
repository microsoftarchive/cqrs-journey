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
    using Registration.Database;
    using Registration.Events;
    using Xunit;
    using Common;
    using System.Collections.Generic;
    using Moq;

    public class SeatsAvailabilityRepositoryFixture
    {
        public SeatsAvailabilityRepositoryFixture()
        {
            using (var context = new RegistrationDbContext("TestOrmRepository"))
            {
                if (context.Database.Exists()) context.Database.Delete();

                context.Database.Create();
            }
        }

        [Fact]
        public void WhenSavingEntity_ThenCanRetrieveIt()
        {
            var id = Guid.NewGuid();

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", Mock.Of<IEventBus>()))
            {
                var aggregate = new SeatsAvailability(id);
                context.Save(aggregate);
            }

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", Mock.Of<IEventBus>()))
            {
                var aggregate = context.Find(id);

                Assert.NotNull(aggregate);
            }
        }

        [Fact]
        public void WhenSavingEntityTwice_ThenCanReloadIt()
        {
            var id = Guid.NewGuid();

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", Mock.Of<IEventBus>()))
            {
                var aggregate = new SeatsAvailability(id);
                context.Save(aggregate);
            }

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", Mock.Of<IEventBus>()))
            {
                var aggregate = context.Find(id);
                aggregate.AddSeats(Guid.NewGuid(), 10);

                context.Save(aggregate);
            }

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", Mock.Of<IEventBus>()))
            {
                var aggregate = context.Find(id);

                Assert.Equal(1, aggregate.Seats.Count);
                Assert.Equal(10, aggregate.Seats[0].RemainingSeats);
            }
        }

        [Fact]
        public void WhenEntityExposesEvent_ThenRepositoryPublishesIt()
        {
            var id = Guid.NewGuid();
            var seatType = Guid.NewGuid();
            var bus = new Mock<IEventBus>();
            var events = new List<IEvent>();

            bus.Setup(x => x.Publish(It.IsAny<IEnumerable<IEvent>>()))
                .Callback<IEnumerable<IEvent>>(events.AddRange);

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", bus.Object))
            {
                var aggregate = new SeatsAvailability(id);
                aggregate.AddSeats(seatType, 10);
                context.Save(aggregate);
            }

            using (var context = new SeatsAvailabilityRepository("TestOrmRepository", bus.Object))
            {
                var aggregate = context.Find(id);
                aggregate.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(seatType, 1) });
                context.Save(aggregate);
            }

            Assert.Equal(1, events.Count);
            Assert.IsAssignableFrom(typeof(SeatsReserved), events[0]);
        }
    }
}

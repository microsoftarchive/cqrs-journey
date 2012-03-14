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

namespace Registration.IntegrationTests
{
    using System;
    using System.Data.Entity;
    using Registration.Database;
    using Registration.ReadModel;
    using Xunit;

    public class OrmViewRepositoryFixture
    {
        [Fact]
        public void WhenReadingViewDTO_ThenSucceedsIfAggregateExists()
        {
            Database.SetInitializer<OrmRepository>(
                new OrmViewRepositoryInitializer(
                    new OrmRepositoryInitializer(
                        new DropCreateDatabaseAlways<OrmRepository>())));
            Database.SetInitializer<OrmViewRepository>(null);

            using (var context = new OrmRepository("TestOrmRepository"))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Initialize(true);
            }

            var orderId = Guid.NewGuid();
            var ticketTypeId = Guid.NewGuid();

            using (var context = new OrmRepository("TestOrmRepository"))
            {
                var order = new Order(orderId, Guid.NewGuid(), Guid.NewGuid(), new[] { new TicketOrderLine(ticketTypeId, 5) });
                order.MarkAsBooked();
                context.Save(order);
            }

            using (var viewContext = new OrmViewRepository("TestOrmRepository"))
            {
                var dto = viewContext.Find<OrderDTO>(orderId);

                Assert.NotNull(dto);
                Assert.Equal("Booked", dto.State);
                Assert.Equal(1, dto.Lines.Count);
                Assert.Equal(ticketTypeId, dto.Lines[0].SeatTypeId);
                Assert.Equal(5, dto.Lines[0].Quantity);
            }
        }
    }
}

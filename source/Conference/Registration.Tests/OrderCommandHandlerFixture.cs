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

namespace Registration.Tests.OrderCommandHandlerFixture
{
    using System;
    using Common;
    using Moq;
    using Registration.Commands;
    using Registration.Handlers;
    using Xunit;

    public class given_a_handler
    {
        private Mock<IRepository> repository;
        private OrderCommandHandler handler;

        public given_a_handler()
        {
            this.repository = new Mock<IRepository>();
            this.handler = new OrderCommandHandler(() => this.repository.Object);
        }

        [Fact]
        public void when_register_to_conference_then_creates_new_order()
        {
            Order order = null;

            this.repository.Setup(x => x.Save<Order>(It.IsAny<Order>()))
                .Callback<Order>(o => order = o);

            this.handler.Handle(new RegisterToConference
            {
                OrderId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                Seats =
                {
                    new SeatQuantity(Guid.NewGuid(), 10),
                }
            });

            Assert.NotNull(order);
            Assert.Equal(1, order.Items.Count);
            Assert.Equal(10, order.Items[0].Quantity);
        }
    }

    public class given_an_existing_order
    {
        private Mock<IRepository> repository;
        private Order order;
        private OrderCommandHandler handler;

        public given_an_existing_order()
        {
            this.repository = new Mock<IRepository>();
            this.order = new Order(Guid.NewGuid(), Guid.NewGuid(), new OrderItem[0]);
            this.handler = new OrderCommandHandler(() => this.repository.Object);

            repository.Setup(x => x.Find<Order>(order.Id)).Returns(order);
        }

        [Fact]
        public void when_register_to_conference_then_updates_order()
        {
            this.handler.Handle(new RegisterToConference
            {
                OrderId = this.order.Id,
                ConferenceId = this.order.ConferenceId,
                Seats =
                {
                    new SeatQuantity(Guid.NewGuid(), 10),
                }
            });

            Assert.Equal(1, this.order.Items.Count);
            Assert.Equal(10, this.order.Items[0].Quantity);
        }
    }
}

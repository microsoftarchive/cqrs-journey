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

namespace Conference.Web.Public.Tests.Controllers.OrderControllerFixture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Web.Public.Controllers;
    using Infrastructure.Messaging;
    using Moq;
    using Registration;
    using Registration.Commands;
    using Registration.ReadModel;
    using Xunit;

    public class given_controller
    {
        protected readonly OrderController sut;
        protected readonly Mock<IOrderDao> orderDao;
        protected readonly List<ICommand> commands = new List<ICommand>();

        public given_controller()
        {
            this.orderDao = new Mock<IOrderDao>();

            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
               .Callback<Envelope<ICommand>>(x => commands.Add(x.Body));
            bus.Setup(x => x.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
               .Callback<IEnumerable<Envelope<ICommand>>>(x => commands.AddRange(x.Select(e => e.Body)));

            this.sut = new OrderController(Mock.Of<IConferenceDao>(), this.orderDao.Object, bus.Object);

            var routeData = new RouteData();
            routeData.Values.Add("conferenceCode", "conference");

            this.sut.ControllerContext = Mock.Of<ControllerContext>(x => x.RouteData == routeData);
        }

        [Fact]
        public void when_displaying_invalid_order_id_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.Display(Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact]
        public void when_find_order_then_renders_view()
        {
            // Act
            var result = (ViewResult)this.sut.Find();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);
        }

        [Fact]
        public void when_find_order_with_valid_email_and_access_code_then_redirects_to_display_with_order_id()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            this.orderDao.Setup(r => r.LocateOrder("info@contoso.com", "asdf")).Returns(orderId);

            // Act
            var result = (RedirectToRouteResult)this.sut.Find("info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Display", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);
        }

        [Fact]
        public void when_find_order_with_invalid_locator_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.Find("info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact]
        public void when_display_valid_order_then_renders_view_with_priced_order()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var dto = new PricedOrder
            {
                OrderId = orderId,
                Total = 200,
            };

            this.orderDao.Setup(r => r.FindPricedOrder(orderId)).Returns(dto);

            // Act
            var result = (ViewResult)this.sut.Display(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto, result.Model);
        }
    }

    public class given_an_order_seat_assignments : given_controller
    {
        private OrderSeats orderSeats;

        public given_an_order_seat_assignments()
        {
            // Arrange
            this.orderSeats = new OrderSeats
            {
                AssignmentsId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Seats =
                {
                    new OrderSeat(0, "General") 
                    { 
                        Attendee = new PersonalInfo
                        {
                            Email= "a@a.com",
                            FirstName = "A", 
                            LastName = "Z",
                        }
                    },
                    new OrderSeat(1, "Precon"),
                }
            };

            this.orderDao.Setup(r => r.FindOrderSeats(this.orderSeats.OrderId)).Returns(this.orderSeats);
        }

        [Fact]
        public void when_displaying_seat_assignment_then_displays_order_seats()
        {
            // Act
            var result = (ViewResult)this.sut.AssignSeats(this.orderSeats.OrderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(this.orderSeats, result.Model);
        }

        [Fact]
        public void when_no_seat_assignments_for_order_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact]
        public void when_assigning_seats_non_existent_order_id_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(Guid.NewGuid(), new List<OrderSeat>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact]
        public void when_seat_assigned_then_sends_assign_command()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(this.orderSeats.OrderId, new List<OrderSeat>
                {
                    new OrderSeat(1, "Precon") 
                    { 
                        Attendee = new PersonalInfo
                        { 
                            Email = "a@a.com", 
                            FirstName = "A",
                            LastName = "Z",
                        }
                    },
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Display", result.RouteValues["action"]);

            var cmd = this.commands.OfType<AssignSeat>().Single();
            Assert.Equal(this.orderSeats.AssignmentsId, cmd.SeatAssignmentsId);
            Assert.Equal(1, cmd.Position);
            Assert.Equal("a@a.com", cmd.Attendee.Email);
            Assert.Equal("A", cmd.Attendee.FirstName);
            Assert.Equal("Z", cmd.Attendee.LastName);
        }

        [Fact]
        public void when_invalid_position_seat_assigned_then_ignores_it()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(this.orderSeats.OrderId, new List<OrderSeat>
                {
                    new OrderSeat(5, "Precon") 
                    { 
                        Attendee = new PersonalInfo
                        { 
                            Email = "a@a.com", 
                            FirstName = "A",
                            LastName = "Z",
                        }
                    },
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Display", result.RouteValues["action"]);

            Assert.Equal(0, this.commands.Count);
        }

        [Fact]
        public void when_null_seat_assigned_then_ignores_it()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(this.orderSeats.OrderId, new List<OrderSeat>
                {
                   null
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Display", result.RouteValues["action"]);

            Assert.Equal(0, this.commands.Count);
        }

        [Fact]
        public void when_seat_assigned_email_remains_null_then_ignores_it()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(this.orderSeats.OrderId, new List<OrderSeat>
                {
                   new OrderSeat { Position = 1, Attendee = new PersonalInfo() },
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Display", result.RouteValues["action"]);

            Assert.Equal(0, this.commands.Count);
        }

        [Fact]
        public void when_previously_assigned_seat_email_becomes_null_then_sends_unassign_command()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(this.orderSeats.OrderId, new List<OrderSeat>
                {
                   new OrderSeat 
                   { 
                       Position = 0,
                       Attendee = new PersonalInfo
                       {
                           Email = null,
                           FirstName = "A", 
                           LastName = "Z",
                       },
                   }
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Display", result.RouteValues["action"]);

            var cmd = this.commands.OfType<UnassignSeat>().Single();
            Assert.Equal(this.orderSeats.AssignmentsId, cmd.SeatAssignmentsId);
            Assert.Equal(0, cmd.Position);
        }

        [Fact]
        public void when_previously_assigned_seat_firstname_changes_then_sends_assign_seat()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.AssignSeats(this.orderSeats.OrderId, new List<OrderSeat>
                {
                   new OrderSeat 
                   { 
                       Position = 0,
                       Attendee = new PersonalInfo
                       {
                           Email = "a@a.com",
                           FirstName = "B", 
                           LastName = "Z",
                       },
                   }
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Display", result.RouteValues["action"]);

            var cmd = this.commands.OfType<AssignSeat>().Single();
            Assert.Equal(this.orderSeats.AssignmentsId, cmd.SeatAssignmentsId);
            Assert.Equal(0, cmd.Position);
            Assert.Equal("a@a.com", cmd.Attendee.Email);
            Assert.Equal("B", cmd.Attendee.FirstName);
            Assert.Equal("Z", cmd.Attendee.LastName);
        }
    }
}

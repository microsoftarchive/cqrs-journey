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

namespace Conference.Web.Public.Tests.Controllers.RegistrationControllerFixture
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Web.Public.Controllers;
    using Conference.Web.Public.Models;
    using Infrastructure.Messaging;
    using Moq;
    using Payments.Contracts.Commands;
    using Registration;
    using Registration.Commands;
    using Registration.ReadModel;
    using Xunit;

    public class given_controller
    {
        protected readonly RegistrationController sut;
        protected readonly ICommandBus bus;
        protected readonly IOrderDao orderDao;
        protected readonly IConferenceDao conferenceDao;
        protected readonly ConferenceAlias conferenceAlias = new ConferenceAlias { Id = Guid.NewGuid(), Code = "TestConferenceCode", Name = "Test Conference name" };
        protected readonly RouteCollection routes;
        protected readonly RouteData routeData;
        protected readonly Mock<HttpRequestBase> requestMock;
        protected readonly Mock<HttpResponseBase> responseMock;

        public given_controller()
        {
            this.bus = Mock.Of<ICommandBus>();
            this.conferenceDao = Mock.Of<IConferenceDao>(x => x.GetConferenceAlias(conferenceAlias.Code) == conferenceAlias);
            this.orderDao = Mock.Of<IOrderDao>();

            this.routes = new RouteCollection();

            this.routeData = new RouteData();
            this.routeData.Values.Add("conferenceCode", conferenceAlias.Code);

            var requestMock = new Mock<HttpRequestBase>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/request", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());

            var responseMock = new Mock<HttpResponseBase>(MockBehavior.Strict);
            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

            var context = Mock.Of<HttpContextBase>(c => c.Request == requestMock.Object && c.Response == responseMock.Object);

            this.sut = new RegistrationController(this.bus, this.orderDao, this.conferenceDao);
            this.sut.ConferenceAlias = conferenceAlias;
            this.sut.ConferenceCode = conferenceAlias.Code;
            this.sut.ControllerContext = new ControllerContext(context, this.routeData, this.sut);
            this.sut.Url = new UrlHelper(new RequestContext(context, this.routeData), this.routes);
        }

        [Fact]
        public void when_starting_registration_then_returns_view_with_registration_for_conference()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, conferenceAlias.Id, "Test Seat", "Description", 10, 50) };

            // Arrange
            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            // Act
            var result = (ViewResult)this.sut.StartRegistration().Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);

            var resultModel = (OrderViewModel)result.Model;
            Assert.NotNull(resultModel);
            Assert.Equal(1, resultModel.Items.Count);
            Assert.Equal("Test Seat", resultModel.Items[0].SeatType.Name);
            Assert.Equal("Description", resultModel.Items[0].SeatType.Description);
            Assert.Equal(0, resultModel.Items[0].OrderItem.RequestedSeats);
            Assert.Equal(0, resultModel.Items[0].OrderItem.ReservedSeats);
        }

        [Fact]
        public void when_specifying_seats_for_a_valid_registration_then_places_registration_and_redirects_to_action()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, conferenceAlias.Id, "Test Seat", "Description", 10, 50) { AvailableQuantity = 50 } };

            // Arrange
            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();

            Mock.Get(this.orderDao).Setup(r => r.FindDraftOrder(orderId)).Returns(new DraftOrder(orderId, conferenceAlias.Id, DraftOrder.States.PendingReservation));

            var registration =
                new RegisterToConference
                {
                    OrderId = orderId,
                    Seats = { new SeatQuantity(seatTypeId, 10) }
                };

            // Act
            var result = (RedirectToRouteResult)this.sut.StartRegistration(registration, 0);

            // Assert
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("SpecifyRegistrantAndPaymentDetails", result.RouteValues["action"]);
            Assert.Equal(conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);

            Mock.Get<ICommandBus>(this.bus)
                .Verify(
                    b =>
                        b.Send(It.Is<Envelope<ICommand>>(e =>
                            ((RegisterToConference)e.Body).ConferenceId == conferenceAlias.Id
                                && ((RegisterToConference)e.Body).OrderId == orderId
                                && ((RegisterToConference)e.Body).Seats.Count == 1
                                && ((RegisterToConference)e.Body).Seats.ElementAt(0).Quantity == 10
                                && ((RegisterToConference)e.Body).Seats.ElementAt(0).SeatType == seatTypeId)),
                    Times.Once());
        }

        [Fact]
        public void when_initiating_payment_for_a_partially_reserved_order_then_redirects_back_to_seat_selection()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, conferenceAlias.Id, "Test Seat", "Description", 10, 50) };

            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();
            var orderVersion = 10;

            Mock.Get(this.orderDao)
                .Setup(r => r.FindDraftOrder(orderId))
                .Returns(
                    new DraftOrder(orderId, conferenceAlias.Id, DraftOrder.States.PartiallyReserved, orderVersion)
                    {
                        Lines = { new DraftOrderItem(seatTypeId, 10) { ReservedSeats = 5 } }
                    });

            var result = (RedirectToRouteResult)this.sut.StartPayment(orderId, RegistrationController.ThirdPartyProcessorPayment, orderVersion - 1).Result;

            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("StartRegistration", result.RouteValues["action"]);
            Assert.Equal(this.conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);
            Assert.Equal(orderVersion, result.RouteValues["orderVersion"]);
        }

        [Fact]
        public void when_displaying_payment_and_registration_information_for_a_not_yet_updated_order_then_shows_wait_page()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, conferenceAlias.Id, "Test Seat", "Description", 10, 50) };

            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();
            var orderVersion = 10;

            Mock.Get<IOrderDao>(this.orderDao)
                .Setup(d => d.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, Total = 100, OrderVersion = orderVersion });
            var result = (ViewResult)this.sut.SpecifyRegistrantAndPaymentDetails(orderId, orderVersion).Result;

            Assert.Equal("PricedOrderUnknown", result.ViewName);
        }

        [Fact]
        public void when_displaying_payment_and_registration_information_for_a_fully_reserved_order_then_shows_input_page()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, conferenceAlias.Id, "Test Seat", "Description", 10, 50) { AvailableQuantity = 50 } };

            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();
            var orderVersion = 10;

            Mock.Get(this.orderDao)
                .Setup(r => r.FindDraftOrder(orderId))
                .Returns(
                    new DraftOrder(orderId, conferenceAlias.Id, DraftOrder.States.ReservationCompleted, orderVersion)
                    {
                        Lines = { new DraftOrderItem(seatTypeId, 10) { ReservedSeats = 5 } }
                    });
            Mock.Get(this.orderDao)
                .Setup(r => r.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, OrderVersion = orderVersion, ReservationExpirationDate = DateTime.UtcNow.AddMinutes(1) });

            var result = (ViewResult)this.sut.SpecifyRegistrantAndPaymentDetails(orderId, orderVersion - 1).Result;

            Assert.Equal(string.Empty, result.ViewName);
            var model = (RegistrationViewModel)result.Model;
        }

        [Fact]
        public void when_specifying_registrant_and_credit_card_payment_details_for_a_valid_registration_then_sends_commands_and_redirects_to_payment_action()
        {
            var orderId = Guid.NewGuid();
            var command = new AssignRegistrantDetails
            {
                OrderId = orderId,
                Email = "info@contoso.com",
                FirstName = "First Name",
                LastName = "Last Name",
            };
            InitiateThirdPartyProcessorPayment paymentCommand = null;

            // Arrange
            var seatId = Guid.NewGuid();

            var order = new DraftOrder(orderId, conferenceAlias.Id, DraftOrder.States.ReservationCompleted, 10);
            order.Lines.Add(new DraftOrderItem(seatId, 5) { ReservedSeats = 5 });
            Mock.Get<IOrderDao>(this.orderDao)
                .Setup(d => d.FindDraftOrder(orderId))
                .Returns(order);
            Mock.Get<IOrderDao>(this.orderDao)
                .Setup(d => d.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, Total = 100, OrderVersion = 10});

            Mock.Get<ICommandBus>(this.bus)
                .Setup(b => b.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(
                    es => { if (es.Body is InitiateThirdPartyProcessorPayment) paymentCommand = (InitiateThirdPartyProcessorPayment)es.Body; });

            this.routes.MapRoute("ThankYou", "thankyou", new { controller = "Registration", action = "ThankYou" });
            this.routes.MapRoute("SpecifyRegistrantAndPaymentDetails", "checkout", new { controller = "Registration", action = "SpecifyRegistrantAndPaymentDetails" });

            // Act
            var result =
                (RedirectToRouteResult)this.sut.SpecifyRegistrantAndPaymentDetails(command, RegistrationController.ThirdPartyProcessorPayment, 0).Result;

            // Assert
            Mock.Get<ICommandBus>(this.bus)
                .Verify(b => b.Send(It.Is<Envelope<ICommand>>(es => es.Body == command)), Times.Once());
            
            Assert.NotNull(paymentCommand);
            Assert.Equal(conferenceAlias.Id, paymentCommand.ConferenceId);
            Assert.Equal(orderId, paymentCommand.PaymentSourceId);
            Assert.InRange(paymentCommand.TotalAmount, 99.9m, 100.1m);

            Assert.Equal("Payment", result.RouteValues["controller"]);
            Assert.Equal("ThirdPartyProcessorPayment", result.RouteValues["action"]);
            Assert.Equal(this.conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.Equal(paymentCommand.PaymentId, result.RouteValues["paymentId"]);
            Assert.True(((string)result.RouteValues["paymentAcceptedUrl"]).StartsWith("/thankyou"));
            Assert.True(((string)result.RouteValues["paymentRejectedUrl"]).StartsWith("/checkout"));
        }

        [Fact]
        public void when_specifying_registrant_and_credit_card_payment_details_for_a_non_yet_updated_order_then_shows_wait_page()
        {
            var orderId = Guid.NewGuid();
            var orderVersion = 10;
            var command = new AssignRegistrantDetails
            {
                OrderId = orderId,
                Email = "info@contoso.com",
                FirstName = "First Name",
                LastName = "Last Name",
            };
            Guid paymentId = Guid.Empty;

            var seatTypeId = Guid.NewGuid();

            Mock.Get(this.orderDao)
                .Setup(r => r.FindDraftOrder(orderId))
                .Returns(
                    new DraftOrder(orderId, conferenceAlias.Id, DraftOrder.States.Confirmed, orderVersion)
                    {
                        Lines = { new DraftOrderItem(seatTypeId, 10) { ReservedSeats = 5 } }
                    });
            Mock.Get<IOrderDao>(this.orderDao)
                .Setup(d => d.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, Total = 100, OrderVersion = orderVersion + 1 });
            var result = (ViewResult)this.sut.SpecifyRegistrantAndPaymentDetails(command, RegistrationController.ThirdPartyProcessorPayment, orderVersion).Result;

            Assert.Equal("ReservationUnknown", result.ViewName);
        }

        //[Fact]
        //public void when_specifying_more_seats_than_available_then_goes_to_notification_page()
        //{
        //    var conferenceId = Guid.NewGuid();
        //    var seatId = Guid.NewGuid();
        //    var conferenceDTO =
        //        new ConferenceDTO(conferenceId, "conference", "Test Conference", "", new[] { new ConferenceSeatDTO(seatId, "Test Seat", 10d) });

        //    // Arrange
        //    Mock.Get<IViewRepository>(this.viewRepository)
        //        .Setup(r => r.Query<ConferenceDTO>())
        //        .Returns(new ConferenceDTO[] { conferenceDTO }.AsQueryable());

        //    var orderId = Guid.NewGuid();

        //    Mock.Get<IViewRepository>(this.viewRepository)
        //        .Setup(r => r.Find<OrderDTO>(orderId))
        //        .Returns(new OrderDTO(orderId, Order.States.Rejected));

        //    var registration =
        //        new global::Conference.Web.Public.Models.Registration
        //        {
        //            Id = orderId,
        //            ConferenceCode = "conference",
        //            Seats = new[] { new Seat { Quantity = 10, SeatId = seatId } }
        //        };

        //    // Act
        //    var result = (ViewResult)this.sut.StartRegistration("conference", registration);

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.Equal("ReservationRejected", result.ViewName);
        //}

        //[Fact]
        //public void when_confirming_a_registration_then_sends_completion_command()
        //{
        //    var conferenceId = Guid.NewGuid();
        //    var seatId = Guid.NewGuid();

        //    var orderId = Guid.NewGuid();

        //    var registration =
        //        new global::Conference.Web.Public.Models.Registration
        //        {
        //            Id = orderId,
        //            ConferenceCode = "conference",
        //            Seats = new[] { new Seat { Quantity = 10, SeatId = seatId } }
        //        };

        //    // Act
        //    var result = (ViewResult)this.sut.ConfirmRegistration("conference", registration);

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.Equal("RegistrationConfirmed", result.ViewName);
        //}
    }
}

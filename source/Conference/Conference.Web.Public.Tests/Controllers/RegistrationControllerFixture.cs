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

namespace Conference.Web.Public.Tests.Controllers.RegistrationControllerFixture
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Common;
    using Conference.Web.Public.Controllers;
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
        protected readonly ConferenceAliasDTO conferenceAlias = new ConferenceAliasDTO { Id = Guid.NewGuid(), Code = "TestConferenceCode", Name = "Test Conference name" };
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
            this.sut.ControllerContext = new ControllerContext(context, this.routeData, this.sut);
            this.sut.Url = new UrlHelper(new RequestContext(context, this.routeData), this.routes);
        }

        [Fact(Skip = "Need to refactor into a testable cross-cutting concern.")]
        public void when_executing_result_then_makes_conference_alias_available_to_view()
        {
            var seats = new[] { new ConferenceSeatTypeDTO(Guid.NewGuid(), "Test Seat", "Description", 10) };
            // Arrange
            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            // Act
            var result = (ViewResult)this.sut.StartRegistration();
            // How to force OnResultExecuting?
            // TODO: instead, can create an action filter an test that cross-cutting concern separately.

            // Assert
            Assert.NotNull(result.ViewData["Conference"]);
            Assert.Same(conferenceAlias, result.ViewData["Conference"]);
        }

        [Fact]
        public void when_starting_registration_then_returns_view_with_registration_for_conference()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new ConferenceSeatTypeDTO(seatTypeId, "Test Seat", "Description", 10) };

            // Arrange
            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            // Act
            var result = (ViewResult)this.sut.StartRegistration();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);

            var resultModel = result.Model as IList<ConferenceSeatTypeDTO>;
            Assert.NotNull(resultModel);
            Assert.Equal(1, resultModel.Count);
            Assert.Equal("Test Seat", resultModel[0].Name);
            Assert.Equal("Description", resultModel[0].Description);
        }

        [Fact]
        public void when_specifying_seats_for_a_valid_registration_then_places_registration_and_redirects_to_action()
        {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new ConferenceSeatTypeDTO(seatTypeId, "Test Seat", "Description", 10) };

            // Arrange
            Mock.Get(this.conferenceDao).Setup(r => r.GetPublishedSeatTypes(conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();

            Mock.Get(this.orderDao).Setup(r => r.GetOrderDetails(orderId)).Returns(new OrderDTO(orderId, OrderDTO.States.Created));

            var registration =
                new RegisterToConference
                {
                    OrderId = orderId,
                    Seats = { new SeatQuantity(seatTypeId, 10) }
                };

            // Act
            var result = (RedirectToRouteResult)this.sut.StartRegistration(registration);

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
        public void when_specifying_registrant_details_for_a_valid_registration_then_sends_command_and_redirects_to_specify_payment_details()
        {
            var orderId = Guid.NewGuid();
            var command = new AssignRegistrantDetails
            {
                OrderId = orderId,
                Email = "info@contoso.com",
                FirstName = "First Name",
                LastName = "Last Name",
            };
            Guid paymentId = Guid.Empty;

            // Arrange
            var seatId = Guid.NewGuid();

            var order = new OrderDTO(orderId, OrderDTO.States.ReservationCompleted);
            order.Lines.Add(new OrderItemDTO(seatId, 5) { ReservedSeats = 5 });
            Mock.Get<IOrderDao>(this.orderDao)
                .Setup(d => d.GetOrderDetails(orderId))
                .Returns(order);

            var seat = new ConferenceSeatTypeDTO(seatId, "test", "description", 20);
            Mock.Get<IConferenceDao>(this.conferenceDao)
                .Setup(d => d.GetPublishedSeatTypes(this.conferenceAlias.Id))
                .Returns(new[] { seat });

            Mock.Get<ICommandBus>(this.bus)
                .Setup(b => b.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
                .Callback<IEnumerable<Envelope<ICommand>>>(
                    es => { paymentId = (es.Select(e => e.Body).OfType<InitiateThirdPartyProcessorPayment>().First()).PaymentId; });

            this.routes.MapRoute("ThankYou", "thankyou", new { controller = "Registration", action = "ThankYou" });

            // Act
            var result =
                (RedirectToRouteResult)this.sut.SpecifyRegistrantAndPaymentDetails(command, RegistrationController.ThirdPartyProcessorPayment);

            // Assert
            Mock.Get<ICommandBus>(this.bus)
                .Verify(
                    b =>
                        b.Send(
                            It.Is<IEnumerable<Envelope<ICommand>>>(es =>
                                es.Select(e => e.Body).Any(c => c == command)
                                && es.Select(e => e.Body).OfType<InitiateThirdPartyProcessorPayment>()
                                     .Any(c =>
                                         c.ConferenceId == conferenceAlias.Id
                                         && c.PaymentSourceId == orderId
                                         && Math.Abs(c.TotalAmount - 100) < 0.01m))),
                    Times.Once());

            Assert.Equal("Payment", result.RouteValues["controller"]);
            Assert.Equal("ThirdPartyProcessorPayment", result.RouteValues["action"]);
            Assert.Equal(this.conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.Equal(paymentId, result.RouteValues["paymentId"]);
            Assert.True(((string)result.RouteValues["paymentAcceptedUrl"]).StartsWith("/thankyou"));
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

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
    using System.Linq;
    using System.Web.Mvc;
    using Common;
    using Conference.Web.Public.Controllers;
    using Conference.Web.Public.Models;
    using Moq;
    using Registration;
    using Registration.Commands;
    using Registration.ReadModel;
    using Xunit;

    public class given_controller
    {
        protected readonly RegistrationController sut;
        protected readonly ICommandBus bus;
        protected readonly IViewRepository viewRepository;
        protected readonly ConferenceAliasDTO conferenceAlias = new ConferenceAliasDTO(Guid.NewGuid(), "TestConferenceCode", "Test Conference name");

        public given_controller()
        {
            this.bus = Mock.Of<ICommandBus>();
            this.viewRepository = Mock.Of<IViewRepository>(x => x.Query<ConferenceAliasDTO>() == new ConferenceAliasDTO[] { conferenceAlias }.AsQueryable());

            this.sut = new RegistrationController(this.bus, () => this.viewRepository);
            this.sut.ControllerContext = new ControllerContext();
            this.sut.ControllerContext.RouteData.Values.Add("conferenceCode", conferenceAlias.Code);
        }

        [Fact(Skip="Need to refactor into a testable cross-cutting concern.")]
        public void when_executing_result_then_makes_conference_alias_available_to_view()
        {
            var conferenceDTO = new ConferenceDTO(conferenceAlias.Id, conferenceAlias.Code, conferenceAlias.Name, "", new[] { new ConferenceSeatTypeDTO(Guid.NewGuid(), "Test Seat", 10d) });

            // Arrange
            Mock.Get<IViewRepository>(this.viewRepository)
                .Setup(r => r.Query<ConferenceDTO>())
                .Returns(new ConferenceDTO[] { conferenceDTO }.AsQueryable());

            // Act
            var result = (ViewResult)this.sut.StartRegistration(conferenceAlias.Code);
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
            var conferenceDTO = new ConferenceDTO(conferenceAlias.Id, conferenceAlias.Code, conferenceAlias.Name, "", new[] { new ConferenceSeatTypeDTO(seatTypeId, "Test Seat", 10d) });

            // Arrange
            Mock.Get<IViewRepository>(this.viewRepository)
                .Setup(r => r.Query<ConferenceDTO>())
                .Returns(new ConferenceDTO[] { conferenceDTO }.AsQueryable());

            // Act
            var result = (ViewResult)this.sut.StartRegistration(conferenceAlias.Code);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);

            var resultModel = result.Model as IList<ConferenceSeatTypeDTO>;
            Assert.NotNull(resultModel);
            Assert.Equal(1, resultModel.Count);
            Assert.Equal("Test Seat", resultModel[0].Description);
        }

        [Fact]
        public void when_specifying_seats_for_a_valid_registration_then_places_registration_and_redirects_to_action()
        {
            var seatTypeId = Guid.NewGuid();
            var conferenceDTO = new ConferenceDTO(conferenceAlias.Id, conferenceAlias.Code, conferenceAlias.Name, "", new[] { new ConferenceSeatTypeDTO(seatTypeId, "Test Seat", 10d) });

            // Arrange
            Mock.Get<IViewRepository>(this.viewRepository)
                .Setup(r => r.Query<ConferenceDTO>())
                .Returns(new ConferenceDTO[] { conferenceDTO }.AsQueryable());

            var orderId = Guid.NewGuid();

            Mock.Get<IViewRepository>(this.viewRepository)
                .Setup(r => r.Find<OrderDTO>(orderId))
                .Returns(new OrderDTO(orderId, Order.States.Created));

            var registration =
                new RegisterToConference
                {
                    OrderId = orderId,
                    Seats = { new SeatQuantity { Quantity = 10, SeatType = seatTypeId } }
                };

            // Act
            var result = (RedirectToRouteResult)this.sut.StartRegistration(conferenceAlias.Code, registration);

            // Assert
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("SpecifyRegistrantDetails", result.RouteValues["action"]);
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
            var conferenceId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var command = new AssignRegistrantDetails
            {
                OrderId = orderId,
                Email = "info@contoso.com",
                FirstName = "First Name",
                LastName = "Last Name",
            };

            // Act
            var result = (RedirectToRouteResult)this.sut.SpecifyRegistrantDetails("conference", command);

            // Assert
            Mock.Get<ICommandBus>(this.bus)
                .Verify(b => b.Send(It.Is<Envelope<ICommand>>(c => c.Body == command)), Times.Once());

            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("SpecifyPaymentDetails", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);
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

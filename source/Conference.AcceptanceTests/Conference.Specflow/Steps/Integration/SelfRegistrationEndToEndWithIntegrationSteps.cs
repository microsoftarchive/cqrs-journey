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

using System;
using System.Threading;
using Conference.Specflow.Support;
using Infrastructure.Messaging;
using Registration;
using Registration.Commands;
using System.Linq;
using Registration.Events;
using Registration.ReadModel;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "SelfRegistrationEndToEndWithIntegration")]
    public class SelfRegistrationEndToEndWithIntegrationSteps
    {
        private readonly ICommandBus commandBus;
        private Guid orderId;
        private RegisterToConference registerToConference;

        public SelfRegistrationEndToEndWithIntegrationSteps()
        {
            commandBus = ConferenceHelper.BuildCommandBus();
        }

        [When(@"the Registrant proceeds to make the Reservation")]
        [When(@"the command to register the selected Order Items is sent")]
        public void WhenTheCommandToRegisterTheSelectedOrderItemsIsSent()
        {
            registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            registerToConference.ConferenceId = conferenceAlias.Id;
            orderId = registerToConference.OrderId;
            this.commandBus.Send(registerToConference);

            // Wait for event processing
            Thread.Sleep(Constants.WaitTimeout);
        }

        [Then(@"the Reservation is confirmed for all the selected Order Items")]
        [Then(@"the event for Order placed is emitted")]
        public void ThenTheEventForOrderPlacedIsEmitted()
        {
            Assert.True(MessageLogHelper.CollectEvents<OrderPlaced>(orderId, 1));

            var orderPlaced = MessageLogHelper.GetEvents<OrderPlaced>(orderId).Single();
            
            Assert.True(orderPlaced.Seats.All(
                os => registerToConference.Seats.Count(cs => cs.SeatType == os.SeatType && cs.Quantity == os.Quantity) == 1));
        }

        [Then(@"the command for reserving the selected Seats is received")]
        [Scope(Tag = "RegistrationProcessHardeningIntegration")]
        public void ThenTheCommandForReservingTheSelectedSeatsIsReceived()
        {
            var repository = EventSourceHelper.GetSeatsAvailabilityRepository();
            var command = ScenarioContext.Current.Get<RegisterToConference>();

            var availability = repository.Find(command.ConferenceId);
            
            Assert.NotNull(availability);
        }

        [Then(@"the event for reserving the selected Seats is emitted")]
        [Scope(Tag = "RegistrationProcessHardeningIntegration")]
        public void ThenTheEventForReservingTheSelectedSeatsIsEmitted()
        {
            registerToConference = registerToConference ?? ScenarioContext.Current.Get<RegisterToConference>();

            // Wait and Check for SeatsReserved event was emitted 
            Assert.True(MessageLogHelper.CollectEvents<SeatsReserved>(registerToConference.ConferenceId, 1));
            var seatsReserved = MessageLogHelper.GetEvents<SeatsReserved>(registerToConference.ConferenceId).SingleOrDefault();

            Assert.NotNull(seatsReserved);
            Assert.Equal(registerToConference.Seats.Count, seatsReserved.AvailableSeatsChanged.Count());
        }

        [Then(@"these Order Items should be reserved")]
        public void ThenTheseOrderItemsShouldBeReserved(Table table)
        {
            var orderReservationCompleted = MessageLogHelper.GetEvents<OrderReservationCompleted>(orderId).SingleOrDefault();
            Assert.NotNull(orderReservationCompleted);

            var conferenceInfo = ScenarioContext.Current.Get<ConferenceInfo>();

            foreach (var row in table.Rows)
            {
                var seat = conferenceInfo.Seats.FirstOrDefault(s => s.Description == row["seat type"]);
                Assert.NotNull(seat);
                Assert.True(orderReservationCompleted.Seats.Any(
                        s => s.SeatType == seat.Id && s.Quantity == int.Parse(row["quantity"])));
            }
        }

        [Then(@"these Order Items should not be reserved")]
        public void ThenTheseOrderItemsShouldNotBeReserved(Table table)
        {
            var orderReservationCompleted = MessageLogHelper.GetEvents<OrderReservationCompleted>(orderId).SingleOrDefault();
            Assert.NotNull(orderReservationCompleted);

            var conferenceInfo = ScenarioContext.Current.Get<ConferenceInfo>();

            foreach (var row in table.Rows)
            {
                var seat = conferenceInfo.Seats.FirstOrDefault(s => s.Description == row["seat type"]);
                Assert.NotNull(seat);
                Assert.False(orderReservationCompleted.Seats.Any(s => s.SeatType == seat.Id));
            }
        }

        [Then(@"the event for completing the Order reservation is emitted")]
        public void ThenTheEventForCompletingTheOrderReservationIsEmitted()
        {
            var orderReservationCompleted = MessageLogHelper.GetEvents<OrderReservationCompleted>(orderId).SingleOrDefault();

            Assert.NotNull(orderReservationCompleted);
            Assert.Equal(registerToConference.Seats.Count, orderReservationCompleted.Seats.Count());
            Assert.True(orderReservationCompleted.ReservationExpiration > DateTime.Now);
        }

        [Then(@"the total should read \$(.*)")]
        [Then(@"the event for calculating the total of \$(.*) is emitted")]
        public void ThenTheEventForCalculatingTheTotalIsEmitted(decimal total)
        {
            var orderTotalsCalculated = MessageLogHelper.GetEvents<OrderTotalsCalculated>(orderId).SingleOrDefault();

            Assert.NotNull(orderTotalsCalculated);
            Assert.Equal(total, orderTotalsCalculated.Total);
            Assert.False(orderTotalsCalculated.IsFreeOfCharge);
        }
    }
}

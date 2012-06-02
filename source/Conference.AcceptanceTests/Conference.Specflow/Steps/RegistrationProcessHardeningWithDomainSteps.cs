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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conference.Common.Utils;
using Conference.Specflow.Support;
using Infrastructure.Messaging;
using Registration;
using Registration.Commands;
using Registration.Events;
using Registration.ReadModel;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "RegistrationProcessHardeningWithDomain")]
    public class RegistrationProcessHardeningWithDomainSteps
    {
        private readonly ICommandBus commandBus;
        private readonly IEventBus eventBus; 
        private Guid orderId;
        private RegisterToConference registerToConference;

        public RegistrationProcessHardeningWithDomainSteps()
        {
            commandBus = ConferenceHelper.BuildCommandBus();
            eventBus = ConferenceHelper.BuildEventBus();
        }

        [When(@"the command to register the selected Order Items is lost")]
        public void WhenTheCommandToRegisterTheSelectedOrderItemsIsLost()
        {
            registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            registerToConference.ConferenceId = conferenceAlias.Id;
            orderId = registerToConference.OrderId;

            //Command lost because of a failure
            //this.commandBus.Send(registerToConference);        }
        }

        [When(@"the event for Order placed is emitted")]
        public void WhenTheEventForOrderPlacedIsEmitted()
        {
            var orderPlaced = new OrderPlaced
            {
                SourceId = orderId,
                ConferenceId = registerToConference.ConferenceId,
                Seats = registerToConference.Seats,
                ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromSeconds(5)),
                AccessCode = HandleGenerator.Generate(6)
            };

            eventBus.Publish(orderPlaced);

            // Wait for event processing
            Thread.Sleep(Constants.WaitTimeout); 
        }

        [When(@"the Registrant proceed to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            registerToConference.ConferenceId = conferenceAlias.Id;
            orderId = registerToConference.OrderId;
            this.commandBus.Send(registerToConference);
        }

        [When(@"the event for Order placed is emitted with a short expiration time")]
        public void WhenTheEventForOrderPlacedIsEmittedWithAShortExpirationTime()
        {
            //Update the registration data
            WhenTheCommandToRegisterTheSelectedOrderItemsIsLost();

            var orderPlaced = new OrderPlaced
            {
                SourceId = orderId,
                ConferenceId = registerToConference.ConferenceId,
                Seats = registerToConference.Seats,
                ReservationAutoExpiration = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(14)),
                AccessCode = HandleGenerator.Generate(6)
            };

            eventBus.Publish(orderPlaced);
        }

        [Then(@"the command for cancelling the reservation is received")]
        public void ThenTheCommandForCancellingTheReservationIsReceived()
        {
            var command = MessageLogHelper.GetCommands<CancelSeatReservation>().
                FirstOrDefault(c => c.ConferenceId == registerToConference.ConferenceId);

            Assert.NotNull(command);
        }

        [Then(@"the command for rejecting the order is received")]
        public void ThenTheCommandForRejectingTheOrderIsReceived()
        {
            var command = MessageLogHelper.GetCommands<RejectOrder>().
                FirstOrDefault(c => c.OrderId == orderId);

            Assert.NotNull(command);
        }
    }
}

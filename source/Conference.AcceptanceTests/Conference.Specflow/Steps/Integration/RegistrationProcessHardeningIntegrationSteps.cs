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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conference.Common.Utils;
using Conference.Specflow.Support;
using Infrastructure.Messaging;
using Payments.Contracts.Commands;
using Payments.Contracts.Events;
using Registration;
using Registration.Commands;
using Registration.Events;
using Registration.ReadModel;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "RegistrationProcessHardeningIntegration")]
    public class RegistrationProcessHardeningIntegrationSteps
    {
        private readonly ICommandBus commandBus;
        private readonly IEventBus eventBus; 
        private Guid orderId;
        private Guid paymentId;
        private RegisterToConference registerToConference;

        public RegistrationProcessHardeningIntegrationSteps()
        {
            commandBus = ConferenceHelper.BuildCommandBus();
            eventBus = ConferenceHelper.BuildEventBus();
        }

        [Given(@"the command to register the selected Order Items is sent")]
        public void GivenTheCommandToRegisterTheSelectedOrderItemsIsSent()
        {
            WhenTheCommandToRegisterTheSelectedOrderItemsIsLost();
            this.commandBus.Send(registerToConference); 
        }

        [When(@"the command to register the selected Order Items is lost")]
        public void WhenTheCommandToRegisterTheSelectedOrderItemsIsLost()
        {
            registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            registerToConference.ConferenceId = conferenceAlias.Id;
            orderId = registerToConference.OrderId;

            //Command lost because of a failure
            //this.commandBus.Send(registerToConference); 
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
        }

        [When(@"the Registrant proceeds to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            registerToConference.ConferenceId = conferenceAlias.Id;
            orderId = registerToConference.OrderId;
            this.commandBus.Send(registerToConference);
        }

        [Given(@"the event for Order placed gets expired")]
        public void GivenTheEventForOrderPlacedGetExpired()
        {
            WhenTheEventForOrderPlacedGetExpired();
        }

        [When(@"the event for Order placed gets expired")]
        public void WhenTheEventForOrderPlacedGetExpired()
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

        [When(@"the command for initiate the payment is sent")]
        public void WhenTheComandForInitiateThePaymentIsSent()
        {
            var paymentCommand =
                 new InitiateThirdPartyProcessorPayment
                 {
                     PaymentId = Guid.NewGuid(),
                     ConferenceId = registerToConference.ConferenceId,
                     PaymentSourceId = orderId,
                     Description = "test",
                     TotalAmount = 249
                 };

            paymentId = paymentCommand.PaymentId;

            commandBus.Send(paymentCommand);
        }

        [When(@"the command for completing the payment process is sent")]
        public void WhenTheCommandForCompletingThePaymentProcessIsSent()
        {
            this.commandBus.Send(new CompleteThirdPartyProcessorPayment { PaymentId = paymentId });
        }

        [Then(@"the event for confirming the payment is emitted")]
        public void ThenTheEventForConfirmingThePaymentIsEmitted()
        {
            Assert.True(MessageLogHelper.CollectEvents<PaymentCompleted>(paymentId, 1));
        }

        //[Then(@"the event for partially confirming the order with no available seats is emitted")]
        //public void ThenTheEventForPartiallyConfirmingTheOrderWithNoAvailableSeatsIsEmitted()
        //{
        //    Assert.True(MessageLogHelper.CollectEvents<OrderPartiallyReserved>(orderId, 1));
        //    var partiallyReserved = MessageLogHelper.GetEvents<OrderPartiallyReserved>(orderId).Single();

        //    // No seats available
        //    Assert.False(partiallyReserved.Seats.Any());
        //}

        [Then(@"the event for confirming the Order is emitted")]
        public void ThenTheEventForConfirmingTheOrderIsEmitted()
        {
            Assert.True(MessageLogHelper.CollectEvents<OrderConfirmed>(orderId, 1));
        }

        [Then(@"the event for confirming the Order is not emitted")]
        public void ThenTheEventForConfirmingTheOrderIsNotEmitted()
        {
            Assert.False(MessageLogHelper.GetEvents<OrderConfirmed>(orderId).ToList().Any());
        }
    }
}

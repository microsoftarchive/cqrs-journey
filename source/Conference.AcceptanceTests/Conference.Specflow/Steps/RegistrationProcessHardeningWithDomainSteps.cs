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
using System.Threading;
using Conference.Specflow.Support;
using Infrastructure.Messaging;
using Registration.Commands;
using Registration.ReadModel;
using TechTalk.SpecFlow;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "RegistrationProcessHardeningWithDomain")]
    public class RegistrationProcessHardeningWithDomainSteps
    {
        private readonly ICommandBus commandBus;
        private Guid orderId;
        private RegisterToConference registerToConference;

        public RegistrationProcessHardeningWithDomainSteps()
        {
            commandBus = ConferenceHelper.BuildCommandBus();
        }

        [When(@"the Registrant proceed to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            registerToConference = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            registerToConference.ConferenceId = conferenceAlias.Id;
            orderId = registerToConference.OrderId;
            this.commandBus.Send(registerToConference);

            // Wait for event processing
            Thread.Sleep(Constants.WaitTimeout);
        }
    }
}

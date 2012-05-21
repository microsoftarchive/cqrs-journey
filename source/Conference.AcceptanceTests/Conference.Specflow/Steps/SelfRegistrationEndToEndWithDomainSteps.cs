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


using System.Threading;
using Conference.Specflow.Support;
using Infrastructure.Messaging;
using Registration.Commands;
using Registration.ReadModel;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class SelfRegistrationEndToEndWithDomainSteps
    {
        private readonly ICommandBus commandBus;

        public SelfRegistrationEndToEndWithDomainSteps()
        {
            commandBus = ConferenceHelper.BuildCommandBus();
        }

        [When(@"the RegisterToConference command is sent")]
        public void WhenTheRegisterToConferenceCommandIsSent()
        {
            var command = ScenarioContext.Current.Get<RegisterToConference>();
            var conferenceAlias = ScenarioContext.Current.Get<ConferenceAlias>();

            command.ConferenceId = conferenceAlias.Id;
            this.commandBus.Send(command);
            
            // Wait for event processing
            Thread.Sleep(Constants.WaitTimeout);
        }

        [Then(@"the OrderUpdated event should be processed and the Order should be persisted")]
        public void ThenTheOrderUpdatedEventShouldBeProcessedAndTheOrderShouldBePersisted()
        {
            var command = ScenarioContext.Current.Get<RegisterToConference>();
            var orderRepo = EventSourceHelper.GetRepository<Registration.Order>();
            Registration.Order order = orderRepo.Find(command.OrderId);

            Assert.NotNull(order);
            Assert.Equal(command.OrderId, order.Id);
        }
    }
}

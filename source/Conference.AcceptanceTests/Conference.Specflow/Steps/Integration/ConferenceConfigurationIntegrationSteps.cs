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

using System.Collections.Generic;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "ConferenceConfigurationIntegration")]
    public class ConferenceConfigurationIntegrationSteps
    {
        private readonly ConferenceService conferenceService;
        private ConferenceInfo conference;
        private ICollection<SeatType> seats;

        public ConferenceConfigurationIntegrationSteps()
        {
            conferenceService = new ConferenceService(ConferenceHelper.BuildEventBus());    
        }

        [Given(@"this conference information")]
        public void GivenThisConferenceInformation(Table table)
        {
            conference = ConferenceHelper.BuildConferenceInfo(table);
        }

        [Given(@"the conference already exists")]
        public void GivenTheConferenceAlreadyExists()
        {
            WhenTheConferenceIsCreated();
        }

        [Given(@"the conference is published")]
        public void GivenTheConferenceIsPublished()
        {
            WhenTheConferenceIsPublished();
        }

        [When(@"these Seat Types are created")]
        public void WhenTheseSeatTypesAreCreated(Table table)
        {
            seats = ConferenceHelper.CreateSeats(table);
            foreach (var seat in seats)
            {
                conferenceService.CreateSeat(conference.Id, seat);
            }
        }

        [When(@"the conference is created")]
        public void WhenTheConferenceIsCreated()
        {
            conferenceService.CreateConference(conference);
        }

        [When(@"the conference is published")]
        public void WhenTheConferenceIsPublished()
        {
            conferenceService.Publish(conference.Id);
        }

        [Then(@"the event for creating the conference is emitted")]
        public void ThenTheEventForCreatingTheConferenceIsEmitted()
        {
            Assert.True(MessageLogHelper.CollectEvents<ConferenceCreated>(conference.Id, 1));
        }

        [Then(@"the event for publishing the conference is emitted")]
        public void ThenTheEventForPublishingTheConferenceIsEmitted()
        {
            Assert.True(MessageLogHelper.CollectEvents<ConferencePublished>(conference.Id, 1));
        }

        [Then(@"the events for creating the Seat Type are emitted")]
        public void ThenTheEventsForCreatingTheSeatTypeAreEmitted()
        {
            foreach (var seat in seats)
            {
                Assert.True(MessageLogHelper.CollectEvents<SeatCreated>(seat.Id, 1));
            }
        }
    }
}

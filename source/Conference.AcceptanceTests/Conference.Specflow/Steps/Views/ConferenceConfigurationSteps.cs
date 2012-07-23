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

using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using W = WatiN.Core;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class ConferenceConfigurationSteps : StepDefinition
    {
        [Given(@"the Business Customer selected the Create Conference option")]
        public void GivenTheBusinessCustomerSelectedTheCreateConferenceOption()
        {
            NavigateToCreateConferenceOption();
        }

        [Given(@"this conference information")]
        public void GivenThisConferenceInformation(Table table)
        {
            PopulateConferenceInformation(table, true);
        }

        [Given(@"the Business Customer proceeds to edit the existing settings with this information")]
        public void GivenTheBusinessCustomerProceedToEditTheExistingSettignsWithThisInformation(Table table)
        {
            Browser.Click(Constants.UI.EditConferenceId);
            PopulateConferenceInformation(table);
        }

        [Given(@"the Business Customer proceeds to create the Conference")]
        public void GivenTheBusinessCustomerProceedToCreateTheConference()
        {
            CreateTheConference();
        }

        [Given(@"an existing published conference with this information")]
        public void GivenAnExistingPublishedConferenceWithThisInformation(Table table)
        {
            ExistingConferenceWithThisInformation(table, true);
        }

        [Given(@"an existing unpublished conference with this information")]
        public void GivenAnExistingUnpublishedConferenceWithThisInformation(Table table)
        {
            ExistingConferenceWithThisInformation(table, false);
        }

        private void ExistingConferenceWithThisInformation(Table table, bool publish)
        {
            NavigateToCreateConferenceOption();
            PopulateConferenceInformation(table, true);
            CreateTheConference();
            if(publish) PublishTheConference();

            ScenarioContext.Current.Set(table.Rows[0]["Email"], Constants.EmailSessionKey);
            ScenarioContext.Current.Set(Browser.FindText(Slug.FindBy), Constants.AccessCodeSessionKey);
        }

        [When(@"the Business Customer proceeds to create the Conference")]
        public void WhenTheBusinessCustomerProceedToCreateTheConference()
        {
            CreateTheConference();
        }

        [When(@"the Business Customer proceeds to publish the Conference")]
        public void WhenTheBusinessCustomerProceedToPublishTheConference()
        {
            PublishTheConference();
        }

        [When(@"the Business Customer proceeds to save the changes")]
        public void WhenTheBusinessCustomerProceedToSaveTheChanges()
        {
            Browser.Click(Constants.UI.UpdateConferenceId);
        }

        [When(@"the Business Customer proceeds to get access to the conference settings")]
        public void WhenTheBusinessCustomerProceedToGetAccessToTheConferenceSettings()
        {
            Browser.GoTo(Constants.ConferenceManagementAccessPage);
            Browser.WaitForComplete((int)Constants.UI.WaitTimeout.TotalSeconds);
            Browser.SetInput("name", ScenarioContext.Current.Get<string>(Constants.EmailSessionKey), "email");
            Browser.SetInput("name", ScenarioContext.Current.Get<string>(Constants.AccessCodeSessionKey), "accessCode");
            Browser.Click(Constants.UI.ConferenceManagementAccessId);
        }

        [Then(@"following details will be shown for the created Conference")]
        public void ThenFollowingDetailsWillBeShownForTheCreatedConference(Table table)
        {
            Assert.True(Browser.SafeContainsText(table.Rows[0]["Owner"]),
                            string.Format("The following text was not found on the page: {0}", table.Rows[0]["Owner"]));
            Assert.True(Browser.SafeContainsText(table.Rows[0]["Email"]),
                string.Format("The following text was not found on the page: {0}", table.Rows[0]["Email"]));

            string ac = Browser.FindText(Slug.FindBy);
            Assert.False(string.IsNullOrWhiteSpace(ac), "Access Code not found");
        }

        [Then(@"the state of the Conference changes to Published")]
        public void ThenTheStateOfTheConferenceChangeToPublished()
        {
            Assert.True(Browser.SafeContainsText(Constants.UI.UnpublishConferenceId), "Conference was not published");
        }

        [Then(@"this information is shown in the Conference settings")]
        public void ThenThisInformationIsShowUpInTheConferenceSettings(Table table)
        {
            Assert.True(Browser.SafeContainsText(table.Rows[0][0]),
                            string.Format("The following text was not found on the page: {0}", table.Rows[0][0]));
        }

        private void PublishTheConference()
        {
            Browser.Click(Constants.UI.PublishConferenceId);
        }

        private void CreateTheConference()
        {
            Browser.Click(Constants.UI.CreateConferenceId);
        }
        
        private void NavigateToCreateConferenceOption()
        {
            // Navigate to Registration page
            Browser.GoTo(Constants.ConferenceManagementCreatePage);
            Browser.WaitForComplete((int)Constants.UI.WaitTimeout.TotalSeconds);
        }

        private void PopulateConferenceInformation(Table table, bool create = false)
        {
            var row = table.Rows[0];

            if (create)
            {
                var slug = Slug.CreateNew().Value;
                Browser.SetInput("OwnerName", row["Owner"]);
                Browser.SetInput("OwnerEmail", row["Email"]);
                Browser.SetInput("name", row["Email"], "ConfirmEmail");
                Browser.SetInput("Slug", slug);
                // Store the conference Slug for future use
                ScenarioContext.Current.Set(slug, "slug");
            }

            Browser.SetInput("Tagline", Constants.UI.TagLine);
            Browser.SetInput("Location", Constants.UI.Location);
            Browser.SetInput("TwitterSearch", Constants.UI.TwitterSearch);

            if (row.ContainsKey("Name")) Browser.SetInput("Name", row["Name"]);
            if (row.ContainsKey("Description")) Browser.SetInput("Description", row["Description"]);
            if (row.ContainsKey("Start")) Browser.SetInput("StartDate", row["Start"]);
            if (row.ContainsKey("End")) Browser.SetInput("EndDate", row["End"]);
        }
    }
}

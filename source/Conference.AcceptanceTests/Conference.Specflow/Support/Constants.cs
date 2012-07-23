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
using System.Configuration;

namespace Conference.Specflow.Support
{
    static class Constants
    {
#if LOCAL
        public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan CommandProcessorWaitTimeout = TimeSpan.FromSeconds(5);
#else   // Wait more for slower Azure connections
        public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan CommandProcessorWaitTimeout = TimeSpan.FromSeconds(60);
#endif
        public const string NoWatiN = "NoWatiN";
        public const string RandomSlug = "(random)";
        public static readonly string ConferenceManagementCreatePage = ConfigurationManager.AppSettings["ConferenceMgmtUrl"] + "create";
        public static readonly string ConferenceManagementAccessPage = ConfigurationManager.AppSettings["ConferenceMgmtUrl"] + "locate";
        public const string EmailSessionKey= "email";
        public const string AccessCodeSessionKey = "accessCode";

        public static class UI
        {
#if LOCAL
            // Max time for wait on a screen to show up
            public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(40); 
#else
            public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(180); 
#endif
            public const string NextStepId = "Next";
            public const string FindId = "find";
            public const string ThirdpartyPayment = "Simulated Third-party Payment Clearing Processor";
            public const string ProceedToSeatAssignementId = "Proceed to Seat Assignment";
            public const string SeatAssignementId = "Assign seats to attendees";
            public const string ReservationSuccessfull = "Seats information";
            public const string ReservationUnsuccessfull = "Could not reserve all the requested seats";
            public const string FindOrderSuccessfull = "Registration details";
            public const string RegistrationSuccessfull = "Thank you";
            public const string AcceptPaymentInputValue = "accepted";
            public const string RejectPaymentInputValue = "rejected";
            public const string SeatAssignmentPage = "Assign Seats";
            public const string TagLine = "Acceptance Tests";
            public const string Location = "Test";
            public const string TwitterSearch = "TwitterSearch";
            public const string ConferenceDescription = "Acceptance Tests CQRS summit 2012 conference";
            public const string CreateConferenceId = "Save conference";
            public const string PublishConferenceId = "Publish";
            public const string UnpublishConferenceId = "Unpublish";
            public const string EditConferenceId = "Edit";
            public const string UpdateConferenceId = "Save";
            public const string ConferenceManagementAccessId = "Login";
            public const string ConferenceManagementSeatTypesId = "Configure seats";
            public const string ConferenceManagementCreateNewSeatTypesId = "Add new seat type";
            public const string ConferenceManagementCreateNewSeatTypeId = "Create";
        }

        public static string RegistrationPage(string conferenceSlug)
        {
            return string.Format(ConfigurationManager.AppSettings["ConferenceUrl"], conferenceSlug, "register");
        }

        public static string FindOrderPage(string conferenceSlug)
        {
            return string.Format(ConfigurationManager.AppSettings["ConferenceUrl"], conferenceSlug, "order/find");
        }
    }
}

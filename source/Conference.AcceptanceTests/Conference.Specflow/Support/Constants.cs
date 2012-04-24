using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Conference.Specflow
{
    static class Constants
    {
        public static readonly string RegistrationPage = string.Format(ConfigurationManager.AppSettings["registrationUrl"], ConferenceSlug);
        public const string ConferenceSlug = "testsite";

        public static class UI
        {
            public const string NextStepButtonID = "Next";
            public const string ReservationSucessfull = "Seats information";
            public const string RegistrationSucessfull = "You will receive a confirmation e-mail in a few minutes.";
            public const string AcceptPaymentInputValue = "accepted";
            public const string RejectPaymentInputValue = "rejected";
        }
    }
}

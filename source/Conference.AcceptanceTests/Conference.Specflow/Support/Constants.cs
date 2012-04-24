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
            public const string RegistrationOrderButtonID = "finish";
            public const string RegistrationSucessfull = "Complete the registration before the count down expires";
        }
    }
}

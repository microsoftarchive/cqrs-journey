using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using WatiN.Core;

namespace Conference.Specflow
{
    [Binding]
    public static class ScenarioContextExtension
    {
        [BeforeScenario]
        public static void BeforeScenario()
        {
            Browser browser = new IE() { Visible = true };
            ScenarioContext.Current.Set(browser);
        }

        [AfterScenario]
        public static void AfterScenario()
        {
            Browser browser = ScenarioContext.Current.Get<Browser>();            
            browser.Close();
        }
    }
}

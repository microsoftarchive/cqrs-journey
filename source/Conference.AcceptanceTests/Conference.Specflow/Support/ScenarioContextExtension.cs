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
        static readonly string key = Guid.NewGuid().ToString();

        public static IE Browser(this ScenarioContext context)
        {
            if (!context.ContainsKey(key))
            {   
                //TODO: Set Visible false for production
                context[key] = new IE() { AutoClose = true, Visible = true };
            }
            return context[key] as IE;
        }

        [AfterScenario]
        static void CloseBrowser()
        {
            if (ScenarioContext.Current.ContainsKey(key))
            {
                var instance = (IE)ScenarioContext.Current[key];
                instance.Close();
            }
        }
    }
}

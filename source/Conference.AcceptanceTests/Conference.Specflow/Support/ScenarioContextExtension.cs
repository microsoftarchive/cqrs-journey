using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using WatiN.Core;
using Xunit;

namespace Conference.Specflow
{
    public static class ScenarioContextExtension
    {
        static readonly string key = Guid.NewGuid().ToString();

        public static IE Browser(this ScenarioContext context)
        {
            if (!context.ContainsKey(key))
            {
                context[key] = new IE();
            }
            return context[key] as IE;
        }

        //[AfterScenario]
        //static void CloseBrowser()
        //{
        //    if (ScenarioContext.Current.ContainsKey(key))
        //    {
        //        var instance = (IE)ScenarioContext.Current[key];
        //        instance.ForceClose();
        //        instance.Dispose();
        //    }
        //}
    }
}

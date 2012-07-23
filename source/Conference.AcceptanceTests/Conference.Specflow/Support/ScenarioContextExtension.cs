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
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;
using WatiN.Core;
using WatiN.Core.Native.Chrome;
using WatiN.Core.Native.Mozilla;

namespace Conference.Specflow.Support
{
    [Binding]
    public static class ScenarioContextExtension
    {
        private readonly static object syncLock = new object();
        private readonly static string BrowsersKey = Guid.NewGuid().ToString();

        [BeforeScenario]
        public static void BeforeScenario()
        {
            if (!FeatureContext.Current.FeatureInfo.Tags.Contains(Constants.NoWatiN) &&
                !ScenarioContext.Current.ScenarioInfo.Tags.Contains(Constants.NoWatiN))
            {
                ScenarioContext.Current.Set(CreateBrowser());
            }
        }

        [AfterScenario]
        public static void AfterScenario()
        {
            Browser browser;
            if (ScenarioContext.Current.TryGetValue(out browser))
                browser.Dispose();

            lock (syncLock)
            {
                List<Browser> browsers;
                if (ScenarioContext.Current.TryGetValue(BrowsersKey, out browsers))
                {
                    browsers.ForEach(b => b.Dispose());
                    ScenarioContext.Current.Remove(BrowsersKey);
                }
            }
        }

        public static Browser Browser(this ScenarioContext context)
        {
            Browser browser;
            context.TryGetValue(out browser);
            return browser;
        }

        public static Browser NewBrowser(this ScenarioContext context)
        {
            var browser = CreateBrowser(); 
            lock (syncLock)
            {
                List<Browser> browsers;
                if (!context.TryGetValue(BrowsersKey, out browsers))
                {
                    browsers = new List<Browser>();
                    context.Add(BrowsersKey, browsers);
                }
                browsers.Add(browser);
            }
            return browser;
        }

        private static Browser CreateBrowser()
        {
            // Set Visible property as true for showing up IE instance (typically used when debugging). 
            return new IE { Visible = true };
        }
    }
}

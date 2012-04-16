using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;

namespace Conference.Specflow.Steps
{
    internal enum BrowserType
    {
        Default = 0,
        IE = 1,
        Firefox = 2
    }

    internal class BrowserStepHelper
    {
        private readonly Browser browser;

        public BrowserStepHelper(BrowserType browserType = BrowserType.Default)
        {
            switch (browserType)
            {
                case BrowserType.Default:
                case BrowserType.IE:
                    browser = new IE();
                    break;
                case BrowserType.Firefox:
                    browser = new FireFox();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("browserType");
            }
        }
   
        public void NavigateToUrl(string url)
        {
            browser.GoToNoWait(url);
        }

    }
}

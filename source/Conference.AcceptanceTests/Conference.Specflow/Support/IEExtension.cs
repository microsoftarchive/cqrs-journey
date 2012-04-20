using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;

namespace Conference.Specflow
{
    public static class IEExtension
    {
        public static void Click(this IE browser, string controlName)
        {
            var link = browser.Link(Find.ById(controlName));

            if (!link.Exists)
                throw new InvalidOperationException(string.Format("Could not find {0} link on the page", controlName));

            link.Click();
        }
    }
}

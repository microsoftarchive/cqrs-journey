﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WatiN.Core;

namespace Conference.Specflow
{
    public static class IEExtension
    {
        public static void Click(this IE browser, string controlId)
        {
            var link = browser.Link(Find.ById(controlId));

            if (!link.Exists)
            {
                var button = browser.Button(Find.ById(controlId));
                if(!button.Exists)
                    throw new InvalidOperationException(string.Format("Could not find {0} link on the page", controlId));
                button.Click();
                return;
            }

            link.Click();
        }

        public static void SelectListInTableRow(this IE browser, string rowName, string value)
        {
            //var tr = browser.TableRow(Find.ByTextInColumn(rowName, 0));
            var tr = browser.TableRows.FirstOrDefault(r => r.Text.Contains(rowName));
            if (tr != null && tr.Lists.Count > 0)
            {
                //tr.SelectLists.First().Select(value);
                var list = tr.Lists.First();
                var item = list.OwnListItem(Find.ByText(value));
                if (item.Exists)
                    item.Click();
            }
        }

        public static bool SafeContainsText(this IE browser, string text)
        {
            try
            {
                return browser.ContainsText(text);
            }
            catch
            {
                return false;
            }
        }
    }
}
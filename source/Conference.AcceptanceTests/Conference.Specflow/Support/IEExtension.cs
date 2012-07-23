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
using System.Linq;
using WatiN.Core;
using WatiN.Core.Exceptions;

namespace Conference.Specflow.Support
{
    public static class IEExtension
    {
        public static void Click(this Browser browser, string controlId)
        {
            var element = FindButton(browser, controlId);
            if (!element.Exists)
            {
                element = FindLink(browser, controlId);
                if (!element.Exists)
                {
                    throw new InvalidOperationException(string.Format(
                        "Could not find {0} link on the page", controlId));
                }
            }
            element.ClickNoWait();
            element.WaitForComplete();
        }

        private static Element FindLink(Browser browser, string value)
        {
            Element element = browser.Link(Find.ById(value));
            if(!element.Exists)
            {
                element = browser.Link(l => l.OuterHtml.Contains(value)); //element = browser.Link(Find.ByText(t => t.Contains(value)));
            }
            return element;
        }

        private static Element FindButton(Browser browser, string value)
        {
            Element element = browser.Button(Find.ById(value));
            if (!element.Exists)
            {
                return browser.Button(b =>
                                         (b.OuterText != null && b.OuterText.Contains(value)) ||
                                         (b.Value != null && b.Value.Contains(value)));
            }
            return element;
        }

        public static void ClickAndWait(this Browser browser, string controlId, string untilContainsText)
        {
            ClickAndWait(browser, controlId, untilContainsText, Constants.UI.WaitTimeout);
        }

        public static void ClickAndWait(this Browser browser, string controlId, string untilContainsText, TimeSpan timeout)
        {
            Click(browser, controlId);
            browser.WaitUntilContainsText(untilContainsText, (int)timeout.TotalSeconds);
        }

        public static void SelectListInTableRow(this Browser browser, string rowName, string value)
        {
            //var tr = browser.TableRow(Find.ByTextInColumn(rowName, 0));
            var tr = browser.TableRows.FirstOrDefault(r => r.Text.Contains(rowName));
            if (tr != null && tr.Lists.Count > 0)
            {
                //tr.SelectLists.First().Select(value);
                var list = tr.Lists.First();
                var item = list.OwnListItem(Find.ByText(value));
                if (item.Exists)
                {
                    item.ClickNoWait();
                    item.WaitForComplete();
                }
            }
        }

        public static bool ContainsValueInTableRow(this Browser browser, string rowName, string value)
        {
            //var tr = browser.TableRow(Find.ByTextInColumn(rowName, 0));
            var tr = browser.TableRows.FirstOrDefault(r => r.Text.Contains(rowName));
            if (tr != null && tr.TableCells.Count > 0)
            {
                return tr.TableCells.Any(c => c.Text.Contains(value));
            }
            return false;
        }

        public static bool ContainsListItemsInTableRow(this Browser browser, string rowName, string maxItems)
        {
            return ContainsListItemsInTableRow(browser, rowName, maxItems, null);
        }

        public static bool ContainsListItemsInTableRow(this Browser browser, string rowName, string selected, string message)
        {
            //var tr = browser.TableRow(Find.ByTextInColumn(rowName, 0));
            var tr = browser.TableRows.FirstOrDefault(r => r.Text.Contains(rowName));
            if (tr != null)
            {
                var list = tr.Lists.FirstOrDefault();
                var nextRow = tr.NextSibling as TableRow;
                return (list == null || list.OwnListItems[0].Text == selected) &&
                       (string.IsNullOrWhiteSpace(message) || 
                        tr.OwnTableCells.Any(tc => tc.Text.Contains(message)) || 
                        nextRow.Text.Trim().Contains(message));
            }
            
            return false;
        }

        public static void SetInput(this Browser browser, string inputId, string value, string attributeValue = null)
        {
            var input = browser.TextField(inputId);
            if (!input.Exists)
                input = browser.TextFields.FirstOrDefault(t => t.GetAttributeValue(inputId) == attributeValue);

            if (input != null && input.Exists)
            {
                input.SetAttributeValue("value", value);
            }
        }

        public static void SetRowCells(this Browser browser, string firstCellValue, params string[] cellValues)
        {
            var firstCell = browser.TableCells.FirstOrDefault(
                c => c.Text != null && 
                     c.Text.Trim() == firstCellValue && 
                     ((TableCell)c.NextSibling).TextFields.First().Value == null);

            if (firstCell == null)
                throw new ElementNotFoundException(firstCellValue, "", "", null);

            foreach (var cellValue in cellValues)
            {
                firstCell = firstCell.NextSibling as TableCell;
                firstCell.TextFields.First().Value = cellValue;
            }
        }

        public static bool SafeContainsText(this Browser browser, string text)
        {
            try
            {
                return browser.ContainsText(text);
            }
            catch { return false; }
        }
    }
}

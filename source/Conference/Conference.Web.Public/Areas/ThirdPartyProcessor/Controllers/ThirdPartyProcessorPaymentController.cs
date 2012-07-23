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

namespace Conference.Web.Public.Areas.ThirdPartyProcessor.Controllers
{
    using System.Web.Mvc;

    /// <summary>
    /// Fake 'third party payment processor' web support
    /// </summary>
    public class ThirdPartyProcessorPaymentController : Controller
    {
        private const string returnUrlKey = "returnUrl";
        private const string cancelReturnUrlKey = "cancelReturnUrl";

        [HttpGet]
        public ActionResult Pay(string itemName, decimal itemAmount, string returnUrl, string cancelReturnUrl)
        {
            this.ViewBag.ItemName = itemName;
            this.ViewBag.ItemAmount = itemAmount;
            this.ViewBag.ReturnUrl = returnUrl;
            this.ViewBag.CancelReturnUrl = cancelReturnUrl;

            return View();
        }

        [HttpPost]
        public ActionResult Pay(string paymentResult, string returnUrl, string cancelReturnUrl)
        {
            string url;

            if (paymentResult == "accepted")
            {
                url = returnUrl;
            }
            else
            {
                url = cancelReturnUrl;
            }

            return Redirect(url);
        }
    }
}

// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Conference.Web.Public.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Mvc;
    using Common;
    using Microsoft.Practices.Unity;
    using Registration.ReadModel;

    public class OrderController : Controller
    {
        private Func<IViewRepository> repositoryFactory;

        public OrderController([Dependency("registration")]Func<IViewRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        [HttpGet]
        public ActionResult Display(string conferenceCode, Guid orderId)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var order = repo.Find<OrderDTO>(orderId);
                if (order == null)
                    // TODO: 404?
                    return RedirectToAction("Find", new { conferenceCode = conferenceCode });

                return View(order);
            }
        }

        [HttpGet]
        public ActionResult Find(string conferenceCode)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Find(string conferenceCode, string email, string accessCode)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var order = repo.Query<OrderDTO>()
                    .Where(o => o.RegistrantEmail == email && o.AccessCode == accessCode)
                    .FirstOrDefault();

                if (order == null)
                    // TODO: 404?
                    return RedirectToAction("Find", new { conferenceCode = conferenceCode });

                return RedirectToAction("Display", new { conferenceCode = conferenceCode, orderId = order.OrderId });
            }
        }
    }
}
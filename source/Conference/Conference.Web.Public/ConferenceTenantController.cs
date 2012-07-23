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

namespace Conference.Web.Public
{
    using System.Web.Mvc;
    using Registration.ReadModel;

    public abstract class ConferenceTenantController : AsyncController
    {
        private ConferenceAlias conferenceAlias;
        private string conferenceCode;

        protected ConferenceTenantController(IConferenceDao conferenceDao)
        {
            this.ConferenceDao = conferenceDao;
        }

        public IConferenceDao ConferenceDao { get; private set; }

        public string ConferenceCode
        {
            get
            {
                return this.conferenceCode ??
                    (this.conferenceCode = (string)ControllerContext.RouteData.Values["conferenceCode"]);
            }
            internal set { this.conferenceCode = value; }
        }

        public ConferenceAlias ConferenceAlias
        {
            get
            {
                return this.conferenceAlias ??
                    (this.conferenceAlias = this.ConferenceDao.GetConferenceAlias(this.ConferenceCode));
            }
            internal set { this.conferenceAlias = value; }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (!string.IsNullOrEmpty(this.ConferenceCode) &&
                this.ConferenceAlias == null)
            {
                filterContext.Result = new HttpNotFoundResult("Invalid conference code.");
            }
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);

            if (filterContext.Result is ViewResultBase)
            {
                this.ViewBag.Conference = this.ConferenceAlias;
            }
        }
    }
}
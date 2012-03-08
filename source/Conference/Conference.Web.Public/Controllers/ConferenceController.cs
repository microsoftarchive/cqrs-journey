// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
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

    using Registration.ReadModel;

    public class ConferenceController : Controller
    {
        private Func<IViewRepository> repositoryFactory;

        public ConferenceController()
            : this(MvcApplication.GetService<Func<IViewRepository>>())
        { }

        public ConferenceController(Func<IViewRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        public ActionResult Display(string conferenceCode)
        {
            var conference = this.GetConference(conferenceCode);

            return View(conference);
        }

        private Conference.Web.Public.Models.Conference GetConference(string conferenceCode)
        {
            var conferenceDTO = this.repositoryFactory().Query<ConferenceDTO>().First(c => c.Code == conferenceCode);

            var conference =
                new Conference.Web.Public.Models.Conference { Code = conferenceDTO.Code, Name = conferenceDTO.Name, Description = conferenceDTO.Description };

            return conference;
        }
    }
}

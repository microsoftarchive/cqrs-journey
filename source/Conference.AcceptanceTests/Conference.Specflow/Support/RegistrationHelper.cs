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

using System;
using System.Collections.Specialized;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Common.Entity;
using Conference.Web.Public.Controllers;
using Infrastructure.Serialization;
using Infrastructure.Sql.BlobStorage;
using Moq;
using Payments.ReadModel.Implementation;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Conference.Specflow.Support
{
    static class RegistrationHelper
    {
        static RegistrationHelper()
        {
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);
            Database.SetInitializer<PaymentsReadDbContext>(null);
            Database.SetInitializer<BlobStorageDbContext>(null);
        }

        public static RegistrationController GetRegistrationController(string conferenceCode)
        {
            Func<ConferenceRegistrationDbContext> ctxFactory = () => new ConferenceRegistrationDbContext(ConferenceRegistrationDbContext.SchemaName);
            var orderDao = new OrderDao(ctxFactory, new SqlBlobStorage("BlobStorage"), new JsonTextSerializer());
            var conferenceDao = new ConferenceDao(ctxFactory);
   
            // Setup context mocks
            var requestMock = new Mock<HttpRequestBase>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/request", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());
            var responseMock = new Mock<HttpResponseBase>(MockBehavior.Strict);
            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

            var context = Mock.Of<HttpContextBase>(c => c.Request == requestMock.Object && c.Response == responseMock.Object);

            var routes = new RouteCollection();
            var routeData = new RouteData();
            routeData.Values.Add("conferenceCode", conferenceCode);

            // Create the controller and set context
            var controller = new RegistrationController(ConferenceHelper.BuildCommandBus(), orderDao, conferenceDao);
            controller.ControllerContext = new ControllerContext(context, routeData, controller);
            controller.Url = new UrlHelper(new RequestContext(context, routeData), routes);

            return controller;
        }

        public static PaymentController GetPaymentController()
        {
            var paymentDao = new PaymentDao(() => new PaymentsReadDbContext(PaymentsReadDbContext.SchemaName));
            return new PaymentController(ConferenceHelper.BuildCommandBus(), paymentDao);
        }

        public static OrderController GetOrderController()
        {
            Func<ConferenceRegistrationDbContext> ctxFactory = () => new ConferenceRegistrationDbContext(ConferenceRegistrationDbContext.SchemaName);
            var orderDao = new OrderDao(ctxFactory, new SqlBlobStorage("BlobStorage"), new JsonTextSerializer());
            var conferenceDao = new ConferenceDao(ctxFactory);

            return new OrderController(conferenceDao, orderDao, ConferenceHelper.BuildCommandBus());
        }

        public static T FindInContext<T>(Guid id) where T : class
        {
            using (var context = new ConferenceRegistrationDbContext(ConferenceRegistrationDbContext.SchemaName))
            {
                return context.Find<T>(id);
            }
        }

        public static Registration.ReadModel.Conference FindConference(Guid conferenceId)
        {
            using (var context = new ConferenceRegistrationDbContext(ConferenceRegistrationDbContext.SchemaName))
            {
                return context.Set<Registration.ReadModel.Conference>().Include(x => x.Seats).FirstOrDefault(x => x.Id == conferenceId);
            }            
        }

        public static T GetModel<T>(ActionResult result) where T : class
        {
            var viewResult = result as ViewResultBase;
            return viewResult != null ? viewResult.Model as T : default(T);
        }
    }
}

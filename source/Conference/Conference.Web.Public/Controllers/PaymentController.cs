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

namespace Conference.Web.Public.Controllers
{
    using System;
    using System.Threading;
    using System.Web.Mvc;
    using Infrastructure.Messaging;
    using Payments.Contracts.Commands;
    using Payments.ReadModel;

    public class PaymentController : Controller
    {
        private const int WaitTimeoutInSeconds = 5;

        private readonly ICommandBus commandBus;
        private readonly IPaymentDao paymentDao;

        public PaymentController(ICommandBus commandBus, IPaymentDao paymentDao)
        {
            this.commandBus = commandBus;
            this.paymentDao = paymentDao;
        }

        public ActionResult ThirdPartyProcessorPayment(string conferenceCode, Guid paymentId, string paymentAcceptedUrl, string paymentRejectedUrl)
        {
            var returnUrl = Url.Action("ThirdPartyProcessorPaymentAccepted", new { conferenceCode, paymentId, paymentAcceptedUrl });
            var cancelReturnUrl = Url.Action("ThirdPartyProcessorPaymentRejected", new { conferenceCode, paymentId, paymentRejectedUrl });

            // TODO retrieve payment information from payment read model

            var paymentDTO = this.WaitUntilAvailable(paymentId);
            if (paymentDTO == null)
            {
                return this.View("WaitForPayment");
            }

            var paymentProcessorUrl =
                this.Url.Action(
                    "Pay",
                    "ThirdPartyProcessorPayment",
                    new
                    {
                        area = "ThirdPartyProcessor",
                        itemName = paymentDTO.Description,
                        itemAmount = paymentDTO.TotalAmount,
                        returnUrl,
                        cancelReturnUrl
                    });

            // redirect to external site
            return this.Redirect(paymentProcessorUrl);
        }

        public ActionResult ThirdPartyProcessorPaymentAccepted(string conferenceCode, Guid paymentId, string paymentAcceptedUrl)
        {
            this.commandBus.Send(new CompleteThirdPartyProcessorPayment { PaymentId = paymentId });

            return this.Redirect(paymentAcceptedUrl);
        }

        public ActionResult ThirdPartyProcessorPaymentRejected(string conferenceCode, Guid paymentId, string paymentRejectedUrl)
        {
            this.commandBus.Send(new CancelThirdPartyProcessorPayment { PaymentId = paymentId });

            return this.Redirect(paymentRejectedUrl);
        }

        private ThirdPartyProcessorPaymentDetails WaitUntilAvailable(Guid paymentId)
        {
            var deadline = DateTime.Now.AddSeconds(WaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var paymentDTO = this.paymentDao.GetThirdPartyProcessorPaymentDetails(paymentId);

                if (paymentDTO != null)
                {
                    return paymentDTO;
                }

                Thread.Sleep(500);
            }

            return null;
        }
    }
}

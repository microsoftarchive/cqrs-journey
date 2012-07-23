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

namespace Conference.Web.Public.Tests.Controllers.PaymentControllerFixture
{
    using System;
    using System.Collections.Specialized;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Web.Public.Controllers;
    using Infrastructure.Messaging;
    using Moq;
    using Payments.Contracts.Commands;
    using Payments.ReadModel;
    using Xunit;

    public class given_controller
    {
        private PaymentController sut;
        private Mock<ICommandBus> commandBusMock;
        private Mock<IPaymentDao> paymentDaoMock;

        public given_controller()
        {
            var routes = new RouteCollection();
            routes.MapRoute("PaymentAccept", "accept", new { controller = "Payment", action = "ThirdPartyProcessorPaymentAccepted" });
            routes.MapRoute("PaymentReject", "reject", new { controller = "Payment", action = "ThirdPartyProcessorPaymentRejected" });
            routes.MapRoute("Pay", "payment", new { controller = "ThirdPartyProcessorPayment", action = "Pay" });

            var requestMock = new Mock<HttpRequestBase>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/request", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());

            var responseMock = new Mock<HttpResponseBase>(MockBehavior.Strict);
            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

            var context = Mock.Of<HttpContextBase>(c => c.Request == requestMock.Object && c.Response == responseMock.Object);

            this.commandBusMock = new Mock<ICommandBus>();
            this.paymentDaoMock = new Mock<IPaymentDao>();
            this.sut = new PaymentController(this.commandBusMock.Object, this.paymentDaoMock.Object);
            this.sut.ControllerContext = new ControllerContext(context, new RouteData(), this.sut);
            this.sut.Url = new UrlHelper(new RequestContext(context, new RouteData()), routes);
        }

        [Fact]
        public void when_initiating_third_party_processor_payment_then_redirects_to_thid_party()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            this.paymentDaoMock
                .Setup(dao => dao.GetThirdPartyProcessorPaymentDetails(It.IsAny<Guid>()))
                .Returns(new ThirdPartyProcessorPaymentDetails(Guid.NewGuid(), Payments.ThirdPartyProcessorPayment.States.Initiated, Guid.NewGuid(), "payment", 100));

            // Act
            var result = (RedirectResult)this.sut.ThirdPartyProcessorPayment("conference", paymentId, "accept", "reject");

            // Assert
            Assert.False(result.Permanent);
            Assert.True(result.Url.StartsWith("/payment"));
        }

        [Fact]
        public void when_payment_is_accepted_then_redirects_to_order()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this.sut.ThirdPartyProcessorPaymentAccepted("conference", paymentId, "accept");

            // Assert
            Assert.Equal("accept", result.Url);
            Assert.False(result.Permanent);
        }

        [Fact]
        public void when_payment_is_accepted_then_publishes_command()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this.sut.ThirdPartyProcessorPaymentAccepted("conference", paymentId, "accept");

            // Assert
            this.commandBusMock.Verify(
                cb => cb.Send(It.Is<Envelope<ICommand>>(e => ((CompleteThirdPartyProcessorPayment)e.Body).PaymentId == paymentId)),
                Times.Once());
        }

        [Fact]
        public void when_payment_is_rejected_then_redirects_to_order()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this.sut.ThirdPartyProcessorPaymentRejected("conference", paymentId, "reject");

            // Assert
            Assert.Equal("reject", result.Url);
            Assert.False(result.Permanent);
        }

        [Fact]
        public void when_payment_is_rejected_then_publishes_command()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this.sut.ThirdPartyProcessorPaymentRejected("conference", paymentId, "reject");

            // Assert
            this.commandBusMock.Verify(
                cb => cb.Send(It.Is<Envelope<ICommand>>(e => ((CancelThirdPartyProcessorPayment)e.Body).PaymentId == paymentId)),
                Times.Once());
        }
    }
}

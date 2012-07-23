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

namespace Payments.Tests.ThirdPartyProcessorPaymentCommandHandlerFixture
{
    using System;
    using System.Linq;
    using Infrastructure.Database;
    using Moq;
    using Payments.Contracts.Commands;
    using Payments.Handlers;
    using Xunit;

    public class given_no_payment
    {
        private Mock<IDataContext<ThirdPartyProcessorPayment>> contextMock;
        private ThirdPartyProcessorPaymentCommandHandler handler;

        public given_no_payment()
        {
            this.contextMock = new Mock<IDataContext<ThirdPartyProcessorPayment>>();
            this.handler = new ThirdPartyProcessorPaymentCommandHandler(() => this.contextMock.Object);
        }

        [Fact]
        public void when_initiating_payment_then_adds_new_payment()
        {
            ThirdPartyProcessorPayment payment = null;
            var orderId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var conferenceId = Guid.NewGuid();

            this.contextMock
                .Setup(x => x.Save(It.IsAny<ThirdPartyProcessorPayment>()))
                .Callback<ThirdPartyProcessorPayment>(p => payment = p);

            this.handler.Handle(
                new InitiateThirdPartyProcessorPayment
                {
                    PaymentId = paymentId,
                    PaymentSourceId = orderId,
                    ConferenceId = conferenceId,
                    Items = 
                    { 
                        new InitiateThirdPartyProcessorPayment.PaymentItem{ Description = "payment", Amount = 100 } 
                    }
                });

            Assert.NotNull(payment);
            Assert.Equal(1, payment.Items.Count);
            Assert.Equal("payment", payment.Items.ElementAt(0).Description);
            Assert.Equal(100, payment.Items.ElementAt(0).Amount);
        }
    }

    public class given_initiated_payment
    {
        private Mock<IDataContext<ThirdPartyProcessorPayment>> contextMock;
        private ThirdPartyProcessorPayment payment;
        private ThirdPartyProcessorPaymentCommandHandler handler;

        public given_initiated_payment()
        {
            this.contextMock = new Mock<IDataContext<ThirdPartyProcessorPayment>>();
            this.payment = new ThirdPartyProcessorPayment(Guid.NewGuid(), Guid.NewGuid(), "payment", 100, new ThidPartyProcessorPaymentItem[0]);
            this.handler = new ThirdPartyProcessorPaymentCommandHandler(() => this.contextMock.Object);

            contextMock.Setup(x => x.Find(payment.Id)).Returns(payment);
        }

        [Fact]
        public void when_completing_payment_then_updates_payment()
        {
            this.handler.Handle(
                new CompleteThirdPartyProcessorPayment
                {
                    PaymentId = this.payment.Id
                });

            Assert.Equal(ThirdPartyProcessorPayment.States.Completed, this.payment.State);
            this.contextMock.Verify(r => r.Save(this.payment), Times.Once());
        }

        [Fact]
        public void when_cancelling_payment_then_updates_payment()
        {
            this.handler.Handle(
                new CancelThirdPartyProcessorPayment
                {
                    PaymentId = this.payment.Id
                });

            Assert.Equal(ThirdPartyProcessorPayment.States.Rejected, this.payment.State);
            this.contextMock.Verify(r => r.Save(this.payment), Times.Once());
        }
    }
}

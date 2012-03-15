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

namespace Conference.Web.Public.Tests.Controllers.PaymentControllerFixture
{
    using System;
    using System.Web.Mvc;
    using Conference.Web.Public.Controllers;
    using Xunit;

    public class given_controller
    {
        private PaymentController sut;

        public given_controller()
        {
            this.sut = new PaymentController();
        }

        [Fact]
        public void when_displaying_then_returns_view()
        {
            // Arrange
            var orderId = Guid.NewGuid();

            // Act
            var result = this.sut.Display("conference", orderId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);
        }

        [Fact]
        public void when_accepting_payment_then_returs_redirect()
        {
            // Arrange
            var orderId = Guid.NewGuid();

            // Act
            var result = this.sut.AcceptPayment("conference", orderId) as RedirectToRouteResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.RouteName);
            Assert.Equal("Registration", result.RouteValues["controller"]);
            Assert.Equal("TransactionCompleted", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);
            Assert.Equal("accepted", result.RouteValues["transactionResult"]);
        }

        [Fact]
        public void when_rejecting_payment_then_returs_redirect()
        {
            // Arrange
            var orderId = Guid.NewGuid();

            // Act
            var result = this.sut.RejectPayment("conference", orderId) as RedirectToRouteResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.RouteName);
            Assert.Equal("Registration", result.RouteValues["controller"]);
            Assert.Equal("TransactionCompleted", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);
            Assert.Equal("rejected", result.RouteValues["transactionResult"]);
        }
    }
}

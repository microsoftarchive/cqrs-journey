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

namespace Conference.Web.Public.Tests.Controllers.OrderControllerFixture
{
    using System;
    using System.Web.Mvc;
    using Conference.Web.Public.Controllers;
    using Infrastructure.Messaging;
    using Moq;
    using Registration.ReadModel;
    using Xunit;

    public class given_controller
    {
        protected readonly OrderController sut;
        protected readonly IOrderDao orderDao;
        protected readonly ISeatAssignmentsDao assignmentsDao;

        public given_controller()
        {
            this.orderDao = Mock.Of<IOrderDao>();
            this.assignmentsDao = Mock.Of<ISeatAssignmentsDao>();

            this.sut = new OrderController(this.orderDao, this.assignmentsDao, Mock.Of<ICommandBus>());
        }

        [Fact]
        public void when_displaying_invalid_order_id_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.Display("conference", Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact(Skip = "Changed requirement from refactoring to seat assignments. Pending fix.")]
        public void when_display_valid_order_then_renders_view_with_order_dto()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orderDto = new DraftOrder(orderId, DraftOrder.States.PendingReservation)
            {
                RegistrantEmail = "info@contoso.com",
                AccessCode = "asdf",
            };

            Mock.Get(this.orderDao).Setup(r => r.GetDraftOrder(orderId)).Returns(orderDto);

            // Act
            var result = (ViewResult)this.sut.Display("conference", orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderDto, result.Model);
        }

        [Fact]
        public void when_find_order_then_renders_view()
        {
            // Act
            var result = (ViewResult)this.sut.Find("conference");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);
        }

        [Fact]
        public void when_find_order_with_valid_email_and_access_code_then_redirects_to_display_with_order_id()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            Mock.Get(this.orderDao).Setup(r => r.LocateOrder("info@contoso.com", "asdf")).Returns(orderId);

            // Act
            var result = (RedirectToRouteResult)this.sut.Find("conference", "info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Display", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
            Assert.Equal(orderId, result.RouteValues["orderId"]);
        }

        [Fact]
        public void when_find_order_with_invalid_locator_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.Find("conference", "info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }
    }
}

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
    using System.Collections.Generic;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Conference.Web.Public.Controllers;
    using Infrastructure.Messaging;
    using Moq;
    using Registration.ReadModel;
    using Xunit;

    public class given_controller
    {
        protected readonly OrderController sut;
        protected readonly Mock<IOrderDao> orderDao;
        protected readonly List<ICommand> commands = new List<ICommand>();

        public given_controller()
        {
            this.orderDao = new Mock<IOrderDao>();

            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
               .Callback<Envelope<ICommand>>(x => commands.Add(x.Body));

            this.sut = new OrderController(Mock.Of<IConferenceDao>(), this.orderDao.Object, bus.Object);

            var routeData = new RouteData();
            routeData.Values.Add("conferenceCode", "conference");

            this.sut.ControllerContext = Mock.Of<ControllerContext>(x => x.RouteData == routeData);
        }

        [Fact]
        public void when_displaying_invalid_order_id_then_redirects_to_find()
        {
            // Act
            var result = (RedirectToRouteResult)this.sut.Display(Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact]
        public void when_find_order_then_renders_view()
        {
            // Act
            var result = (ViewResult)this.sut.Find();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("", result.ViewName);
        }

        [Fact]
        public void when_find_order_with_valid_email_and_access_code_then_redirects_to_display_with_order_id()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            this.orderDao.Setup(r => r.LocateOrder("info@contoso.com", "asdf")).Returns(orderId);

            // Act
            var result = (RedirectToRouteResult)this.sut.Find("info@contoso.com", "asdf");

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
            var result = (RedirectToRouteResult)this.sut.Find("info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(null, result.RouteValues["controller"]);
            Assert.Equal("Find", result.RouteValues["action"]);
            Assert.Equal("conference", result.RouteValues["conferenceCode"]);
        }

        [Fact]
        public void when_display_valid_order_then_renders_view_with_priced_order()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var dto = new PricedOrder
            {
                OrderId = orderId,
                Total = 200,
            };

            this.orderDao.Setup(r => r.GetPricedOrder(orderId)).Returns(dto);

            // Act
            var result = (ViewResult)this.sut.Display(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto, result.Model);
        }
    }
}

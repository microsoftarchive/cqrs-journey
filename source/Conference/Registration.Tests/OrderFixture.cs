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

namespace Registration.Tests.OrderFixture
{
	using System;
	using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Registration.Tests;
    using Xunit;

    public class given_no_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid TicketTypeId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();

        private Order sut;
        private IPersistenceProvider sutProvider;

        protected given_no_order(IPersistenceProvider sutProvider)
        {
            this.sutProvider = sutProvider;
        }

        public given_no_order()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_placing_order_then_raises_integration_event()
        {
            var lines = new[] { new TicketOrderLine(TicketTypeId, 5) };
            this.sut = new Order(OrderId, UserId, lines);

            Assert.Single(sut.GetPendingEvents());
            Assert.Equal(OrderId, ((OrderPlaced)sut.GetPendingEvents().Single()).OrderId);
        }
    }
}

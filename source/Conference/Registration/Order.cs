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

namespace Registration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Common;

    public class Order : IAggregateRoot, IEventPublisher
    {
        private List<IEvent> events = new List<IEvent>();

        protected Order()
        {
        }

        public Order(Guid id, Guid userId, IEnumerable<TicketOrderLine> lines)
        {
            this.Id = id;
            this.UserId = userId;
            this.Lines = lines;
            this.events.Add(new OrderPlaced(this.Id));
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public IEnumerable<TicketOrderLine> Lines { get; private set; }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }
    }

    [ComplexType]
    public class TicketOrderLine
    {
        public TicketOrderLine(Guid ticketTypeId, int quantity)
        {
            this.TicketTypeId = ticketTypeId;
            this.Quantity = quantity;
        }

        protected TicketOrderLine()
        {
        }

        public Guid TicketTypeId { get; private set; }

        public int Quantity { get; private set; }
    }
}

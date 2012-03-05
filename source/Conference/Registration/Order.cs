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
    using System.Linq;
    using Common;
    using Registration.Events;

    public class Order : IAggregateRoot, IEventPublisher
    {
        private List<IEvent> events = new List<IEvent>();

        protected Order()
        {
        }

        public Order(Guid id, Guid userId, Guid conferenceId, IEnumerable<TicketOrderLine> lines)
        {
            this.Id = id;
            this.UserId = userId;
            this.ConferenceId = conferenceId;
            this.Lines = lines;

            // TODO: it feels awkward publishing an event with ALL the details for the order.
            // should we just do the following and let the saga handler populate all the info?
            // this.events.Add(new OrderPlaced { OrderId = this.Id });
            this.events.Add(
                new OrderPlaced 
                { 
                    OrderId = this.Id,
                    ConferenceId = this.ConferenceId,
                    UserId = this.UserId,
                    Tickets = this.Lines.Select(x => new OrderPlaced.Ticket { TicketTypeId = x.TicketTypeId, Quantity = x.Quantity }).ToArray()
                });
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public Guid ConferenceId { get; private set; }

        public IEnumerable<TicketOrderLine> Lines { get; private set; }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }
    }

    [ComplexType]
    public class TicketOrderLine
    {
        public TicketOrderLine(string ticketTypeId, int quantity)
        {
            this.TicketTypeId = ticketTypeId;
            this.Quantity = quantity;
        }

        protected TicketOrderLine()
        {
        }

        public string TicketTypeId { get; private set; }

        public int Quantity { get; private set; }
    }
}

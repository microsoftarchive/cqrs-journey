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
    using System.Collections.ObjectModel;
    using System.Linq;
    using Common;
    using Registration.Events;

    public class Order : IAggregateRoot, IEventPublisher
    {
        public static class States
        {
            public const int Created = 0;
            public const int Booked = 1;
            public const int Rejected = 2;
            public const int Confirmed = 3;
        }

        private List<IEvent> events = new List<IEvent>();

        protected Order()
        {
        }

        public Order(Guid id, Guid userId, Guid conferenceId, IEnumerable<TicketOrderLine> lines)
        {
            this.Id = id;
            this.UserId = userId;
            this.ConferenceId = conferenceId;
            this.Lines = new ObservableCollection<TicketOrderLine>(lines);

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

        public virtual Guid Id { get; private set; }

        public virtual Guid UserId { get; private set; }

        public virtual Guid ConferenceId { get; private set; }

        public virtual ObservableCollection<TicketOrderLine> Lines { get; private set; }

        public virtual int State { get; private set; }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }

        public void MarkAsBooked()
        {
            if (this.State != States.Created)
                throw new InvalidOperationException();

            this.State = States.Booked;
        }

        public void Reject()
        {
            if (this.State != States.Created)
                throw new InvalidOperationException();

            this.State = States.Rejected;
        }
    }

    public class TicketOrderLine
    {
        public TicketOrderLine(string ticketTypeId, int quantity)
        {
            this.Id = Guid.NewGuid();

            this.TicketTypeId = ticketTypeId;
            this.Quantity = quantity;
        }

        protected TicketOrderLine()
        {
        }

        public Guid Id { get; private set; }

        public string TicketTypeId { get; private set; }

        public int Quantity { get; private set; }
    }
}

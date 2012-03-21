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

namespace Registration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Common;
    using Registration.Events;

    public class Order : IAggregateRoot, IEventPublisher
    {
        public enum States
        {
            Created = 0,
            Booked = 1,
            Rejected = 2,
            Confirmed = 3,
        }

        private List<IEvent> events = new List<IEvent>();

        protected Order()
        {
            this.Registrant = new Registrant();
            this.AccessCode = HandleGenerator.Generate(5);
        }

        public Order(Guid id, Guid conferenceId, IEnumerable<OrderItem> items)
            : this()
        {
            this.Id = id;
            this.ConferenceId = conferenceId;
            this.Registrant = new Registrant();
            this.Items = new ObservableCollection<OrderItem>(items);

            // TODO: it feels awkward publishing an event with ALL the details for the order.
            // should we just do the following and let the saga handler populate all the info?
            // this.events.Add(new OrderPlaced { OrderId = this.Id });
            this.events.Add(
                new OrderPlaced
                {
                    OrderId = this.Id,
                    ConferenceId = this.ConferenceId,
                    Items = this.Items.Select(x => new OrderPlaced.OrderItem { SeatTypeId = x.SeatTypeId, Quantity = x.Quantity }).ToArray()
                });
        }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }

        public Guid Id { get; private set; }

        public Guid ConferenceId { get; private set; }

        public virtual ObservableCollection<OrderItem> Items { get; private set; }

        public Registrant Registrant { get; private set; }

        /// <summary>
        /// Access code, combined with the registrant email can 
        /// be used to find the order and provide low chances of 
        /// collision as it's also scoped to the conference.
        /// </summary>
        public string AccessCode { get; set; }

        public int StateValue { get; private set; }
        [NotMapped]
        public States State
        {
            get { return (States)this.StateValue; }
            internal set { this.StateValue = (int)value; }
        }

        public void MarkAsBooked()
        {
            if (this.State != States.Created)
                throw new InvalidOperationException();

            this.State = States.Booked;
        }

        public void Reject()
        {
            if (this.State != States.Created && this.State != States.Booked)
                throw new InvalidOperationException();

            this.State = States.Rejected;
        }

        public void AssignRegistrant(string firstName, string lastName, string email)
        {
            this.Registrant = new Registrant
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
            };
        }
    }
}

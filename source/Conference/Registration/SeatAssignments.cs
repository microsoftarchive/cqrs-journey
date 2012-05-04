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
    using System.Linq;
    using AutoMapper;
    using Infrastructure.EventSourcing;
    using Registration.Events;

    public class SeatAssignments : EventSourced
    {
        class SeatAssignment
        {
            public SeatAssignment()
            {
                this.Attendee = new PersonalInfo();
            }
            public int Position { get; set; }
            public Guid SeatType { get; set; }
            public PersonalInfo Attendee { get; set; }
        }

        private Dictionary<int, SeatAssignment> seats = new Dictionary<int, SeatAssignment>();
        private Guid orderId;

        static SeatAssignments()
        {
            Mapper.CreateMap<SeatAssigned, SeatAssignment>();
            Mapper.CreateMap<SeatUnassigned, SeatAssignment>();
            Mapper.CreateMap<SeatAssignmentUpdated, SeatAssignment>();
        }

        public SeatAssignments(Guid orderId, IEnumerable<SeatQuantity> seats)
            : this(Guid.NewGuid())
        {
            // Add as many assignments as seats there are.
            var i = 0;
            var all = new List<SeatAssignmentsCreated.SeatAssignmentInfo>();
            foreach (var seatQuantity in seats)
            {
                for (int j = 0; j < seatQuantity.Quantity; j++)
                {
                    all.Add(new SeatAssignmentsCreated.SeatAssignmentInfo { Position = i++, SeatType = seatQuantity.SeatType });
                }
            }

            base.Update(new SeatAssignmentsCreated { OrderId = orderId, Seats = all });
        }

        public SeatAssignments(Guid id, IEnumerable<IVersionedEvent> history)
            : this(id)
        {
            this.LoadFrom(history);
        }

        private SeatAssignments(Guid id)
            : base(id)
        {
            base.Handles<SeatAssignmentsCreated>(this.OnCreated);
            base.Handles<SeatAssigned>(this.OnSeatAssigned);
            base.Handles<SeatUnassigned>(this.OnSeatUnassigned);

            // NOTE: we need to add an empty Handles here so that the base class can make 
            // sure we didn't omit a handler by mistake.
            base.Handles<SeatAssignmentUpdated>(this.OnSeatAssignmentUpdated);
        }

        public void AssignSeat(int position, PersonalInfo attendee)
        {
            if (string.IsNullOrEmpty(attendee.Email))
                throw new ArgumentNullException("attendee.Email");

            SeatAssignment current;
            if (!this.seats.TryGetValue(position, out current))
                throw new ArgumentOutOfRangeException("position");

            if (!attendee.Email.Equals(current.Attendee.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                if (current.Attendee.Email != null)
                {
                    this.Update(new SeatUnassigned(this.Id) { OrderId = orderId, Position = position, SeatType = current.SeatType });
                }

                this.Update(new SeatAssigned(this.Id)
                {
                    OrderId = orderId,
                    Position = position,
                    SeatType = current.SeatType,
                    Attendee = attendee,
                });
            }
            else if (!string.Equals(attendee.FirstName, current.Attendee.FirstName, StringComparison.InvariantCultureIgnoreCase)
                || !string.Equals(attendee.LastName, current.Attendee.LastName, StringComparison.InvariantCultureIgnoreCase))
            {
                Update(new SeatAssignmentUpdated(this.Id)
                {
                    OrderId = orderId,
                    Position = position,
                    SeatType = current.SeatType,
                    Attendee = attendee,
                });
            }
        }

        public void Unassign(int position)
        {
            SeatAssignment current;
            if (!this.seats.TryGetValue(position, out current))
                throw new ArgumentOutOfRangeException("position");

            if (current.Attendee.Email != null)
            {
                this.Update(new SeatUnassigned(this.Id) { Position = position });
            }
        }

        private void OnCreated(SeatAssignmentsCreated e)
        {
            this.seats = e.Seats.ToDictionary(x => x.Position, x => new SeatAssignment { Position = x.Position, SeatType = x.SeatType });
            this.orderId = e.OrderId;
        }

        private void OnSeatAssigned(SeatAssigned e)
        {
            this.seats[e.Position] = Mapper.Map(e, new SeatAssignment());
        }

        private void OnSeatUnassigned(SeatUnassigned e)
        {
            this.seats[e.Position] = Mapper.Map(e, new SeatAssignment());
        }

        private void OnSeatAssignmentUpdated(SeatAssignmentUpdated e)
        {
            this.seats[e.Position] = Mapper.Map(e, new SeatAssignment
            {
                // The email property is not received for updates, as those 
                // are for the same attendee essentially.
                Attendee = new PersonalInfo { Email = this.seats[e.Position].Attendee.Email }
            });
        }
    }
}

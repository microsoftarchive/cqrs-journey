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
            public int Position { get; set; }
            public Guid SeatType { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }

        private Dictionary<int, SeatAssignment> seats = new Dictionary<int, SeatAssignment>();

        static SeatAssignments()
        {
            Mapper.CreateMap<SeatAssigned, SeatAssignment>();
            Mapper.CreateMap<SeatUnassigned, SeatAssignment>();
            Mapper.CreateMap<SeatAssignmentUpdated, SeatAssignment>();
        }

        public SeatAssignments(Guid id, IEnumerable<SeatQuantity> seats)
            : this(id)
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
           
            base.Update(new SeatAssignmentsCreated { Seats = all });
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

        public void AssignSeat(int position, string email, string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("email");

            SeatAssignment current;
            if (!this.seats.TryGetValue(position, out current))
                throw new ArgumentOutOfRangeException("position");

            if (!email.Equals(current.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                if (current.Email != null)
                {
                    this.Update(new SeatUnassigned(this.Id) { Position = position, SeatType = current.SeatType });
                }

                this.Update(new SeatAssigned(this.Id)
                                {
                                    Position = position,
                                    SeatType = current.SeatType,
                                    Email = email,
                                    FirstName = firstName,
                                    LastName = lastName,
                                });
            }
            else if (!string.Equals(firstName, current.FirstName, StringComparison.InvariantCultureIgnoreCase)
                || !string.Equals(lastName, current.LastName, StringComparison.InvariantCultureIgnoreCase))
            {
                Update(new SeatAssignmentUpdated(this.Id) { Position = position, FirstName = firstName, LastName = lastName, SeatType = current.SeatType });
            }
        }

        public void Unassign(int position)
        {
            SeatAssignment current;
            if (!this.seats.TryGetValue(position, out current))
                throw new ArgumentOutOfRangeException("position");

            if (current.Email != null)
            {
                this.Update(new SeatUnassigned(this.Id) { Position = position });
            }
        }

        private void OnCreated(SeatAssignmentsCreated e)
        {
            this.seats = e.Seats.ToDictionary(x => x.Position, x => new SeatAssignment { Position = x.Position, SeatType = x.SeatType });
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
            this.seats[e.Position] = Mapper.Map(e, new SeatAssignment { Email = this.seats[e.Position].Email });
        }
    }
}

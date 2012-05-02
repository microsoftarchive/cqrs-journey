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
            public Guid AssignmentId { get; set; }
            public Guid SeatType { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }

        private Dictionary<Guid, SeatAssignment> seats = new Dictionary<Guid, SeatAssignment>();

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
            var all = seats.SelectMany(seat =>
                    Enumerable
                        .Range(0, seat.Quantity)
                        .Select(i => new SeatAssignmentsCreated.SeatAssignmentInfo { SeatAssignmentId = Guid.NewGuid(), SeatType = seat.SeatType }));
            base.Update(new SeatAssignmentsCreated { Seats = all.ToList() });
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

        public void AssignSeat(Guid assignmentId, string email, string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("email");

            SeatAssignment current;
            if (!this.seats.TryGetValue(assignmentId, out current))
                throw new ArgumentOutOfRangeException("assignmentId");

            if (!email.Equals(current.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                if (current.Email != null)
                {
                    this.Update(new SeatUnassigned(this.Id) { AssignmentId = assignmentId, SeatType = current.SeatType });
                }

                this.Update(new SeatAssigned(this.Id)
                                {
                                    AssignmentId = assignmentId,
                                    SeatType = current.SeatType,
                                    Email = email,
                                    FirstName = firstName,
                                    LastName = lastName,
                                });
            }
            else if (!string.Equals(firstName, current.FirstName, StringComparison.InvariantCultureIgnoreCase)
                || !string.Equals(lastName, current.LastName, StringComparison.InvariantCultureIgnoreCase))
            {
                Update(new SeatAssignmentUpdated(this.Id) { AssignmentId = assignmentId, FirstName = firstName, LastName = lastName, SeatType = current.SeatType });
            }
        }

        public void Unassign(Guid assignmentId)
        {
            SeatAssignment current;
            if (!this.seats.TryGetValue(assignmentId, out current))
                throw new ArgumentOutOfRangeException("assignmentId");

            if (current.Email != null)
            {
                this.Update(new SeatUnassigned(this.Id) { AssignmentId = assignmentId });
            }
        }

        private void OnCreated(SeatAssignmentsCreated e)
        {
            this.seats = e.Seats.ToDictionary(x => x.SeatAssignmentId, x => new SeatAssignment { AssignmentId = x.SeatAssignmentId, SeatType = x.SeatType });
        }

        private void OnSeatAssigned(SeatAssigned e)
        {
            this.seats[e.AssignmentId] = Mapper.Map(e, new SeatAssignment());
        }

        private void OnSeatUnassigned(SeatUnassigned e)
        {
            this.seats[e.AssignmentId] = Mapper.Map(e, new SeatAssignment());
        }

        private void OnSeatAssignmentUpdated(SeatAssignmentUpdated e)
        {
            this.seats[e.AssignmentId] = Mapper.Map(e, new SeatAssignment { Email = this.seats[e.AssignmentId].Email });
        }
    }
}

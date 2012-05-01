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
    using System.Text;
    using Infrastructure.EventSourcing;
    using Registration.Events;
    using AutoMapper;

    public class SeatAssignments : EventSourced
    {
        private Dictionary<Guid, int> availableSeats = new Dictionary<Guid, int>();

        public SeatAssignments(Guid id, IEnumerable<SeatQuantity> seats)
            : this(id)
        {
            base.Update(new SeatAssignmentsCreated { Seats = seats.ToList() });
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
        }

        public void AssignSeats(IEnumerable<SeatAssignment> assignments)
        {
            // Avoid enumerating twice.
            var cachedAssignments = assignments.ToList();
            var seatGroups = cachedAssignments
                .GroupBy(x => x.SeatType)
                .Select(g => new { SeatType = g.Key, Count = g.Count() });

            // Validate preconditions for the operation.
            foreach (var seatGroup in seatGroups)
            {
                var available = 0;
                if (!availableSeats.TryGetValue(seatGroup.SeatType, out available))
                {
                    throw new ArgumentException(string.Format(
                        "Seat type '{0}' cannot be assigned as it does not exist for the order.",
                        seatGroup.SeatType));
                }

                if (seatGroup.Count > available)
                    throw new ArgumentException("Cannot assign more seats than available.");
            }

            // Raise one event for each assignment.
            cachedAssignments.ForEach(x => base.Update(Mapper.Map(x, new SeatAssignmentAdded(x.Id))));
        }

        public void ReleaseSeats(IEnumerable<SeatAssignment> assignments)
        {
            // Avoid enumerating twice.
            var cachedAssignments = assignments.ToList();
            var seatGroups = cachedAssignments
                .GroupBy(x => x.SeatType)
                .Select(g => new { SeatType = g.Key, Count = g.Count() });

            // Validate preconditions for the operation.
            foreach (var seatGroup in seatGroups)
            {
                var available = 0;
                if (!availableSeats.TryGetValue(seatGroup.SeatType, out available))
                {
                    throw new ArgumentException(string.Format(
                        "Seat type '{0}' cannot be released as it does not exist for the order.",
                        seatGroup.SeatType));
                }
            }

            // Raise one event for each removed assignment.
            cachedAssignments.ForEach(x => base.Update(Mapper.Map(x, new SeatAssignmentRemoved(x.Id))));
        }

        public void UpdateSeats(IEnumerable<SeatAssignment> assignments)
        {
            // Raise one event for each updated assignment.
            foreach (var assignment in assignments)
            {
                base.Update(Mapper.Map(assignment, new SeatAssignmentUpdated(assignment.Id)));
            }
        }

        private void OnCreated(SeatAssignmentsCreated e)
        {
            this.availableSeats = e.Seats.ToDictionary(x => x.SeatType, x => x.Quantity);
        }

        private void OnAssignmentAdded(SeatAssignmentAdded e)
        {
            // A seat was assigned, so we have one less remaining.
            availableSeats[e.SeatType] -= 1;
        }

        private void OnAssignmentRemoved(SeatAssignmentRemoved e)
        {
            // A seat became unassigned again, so we have one more remaining now.
            availableSeats[e.SeatType] += 1;
        }
    }
}

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

namespace Registration.Handlers
{
    using System;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;
    using Infrastructure.EventSourcing;
    using Registration.Commands;

    public class SeatAssignmentsHandler :
        IEventHandler<OrderPaymentConfirmed>,
        ICommandHandler<AssignSeats>,
        ICommandHandler<ReleaseAssignedSeats>,
        ICommandHandler<UpdateAssignedSeats>
    {
        private IEventSourcedRepository<Order> ordersRepo;
        private IEventSourcedRepository<SeatAssignments> assignmentsRepo;

        public SeatAssignmentsHandler(IEventSourcedRepository<Order> ordersRepo, IEventSourcedRepository<SeatAssignments> assignmentsRepo)
        {
            this.ordersRepo = ordersRepo;
            this.assignmentsRepo = assignmentsRepo;
        }

        public void Handle(OrderPaymentConfirmed @event)
        {
            var order = this.ordersRepo.Find(@event.SourceId);
            var assignments = order.CreateSeatAssignments();

            assignmentsRepo.Save(assignments);
        }

        public void Handle(AssignSeats command)
        {
            var assignments = this.assignmentsRepo.Find(command.OrderId);
            if (assignments != null)
            {
                assignments.AssignSeats(command.Seats);
                assignmentsRepo.Save(assignments);
            }
        }

        public void Handle(ReleaseAssignedSeats command)
        {
            var assignments = this.assignmentsRepo.Find(command.OrderId);
            if (assignments != null)
            {
                assignments.ReleaseSeats(command.Seats);
                assignmentsRepo.Save(assignments);
            }
        }

        public void Handle(UpdateAssignedSeats command)
        {
            var assignments = this.assignmentsRepo.Find(command.OrderId);
            if (assignments != null)
            {
                assignments.UpdateSeats(command.Seats);
                assignmentsRepo.Save(assignments);
            }
        }
    }
}

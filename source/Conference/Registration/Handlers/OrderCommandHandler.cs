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
    using System.Linq;
    using Common;
    using Registration.Commands;

    public class OrderCommandHandler :
        ICommandHandler<RegisterToConference>,
        ICommandHandler<MarkSeatsAsReserved>,
        ICommandHandler<RejectOrder>,
        ICommandHandler<AssignRegistrantDetails>
    {
        private readonly IEventSourcedRepository<Order> repository;

        public OrderCommandHandler(IEventSourcedRepository<Order> repository)
        {
            this.repository = repository;
        }

        public void Handle(RegisterToConference command)
        {
            var items = command.Seats.Select(t => new OrderItem(t.SeatType, t.Quantity)).ToList();
            var order = repository.Find(command.OrderId);
            if (order == null)
            {
                order = new Order(command.OrderId, command.ConferenceId, items);
            }
            else
            {
                order.UpdateSeats(items);
            }

            repository.Save(order);
        }

        public void Handle(MarkSeatsAsReserved command)
        {
            var order = repository.Find(command.OrderId);

            if (order != null)
            {
                order.MarkAsReserved(command.Expiration, command.Seats);
                repository.Save(order);
            }
        }

        public void Handle(RejectOrder command)
        {
            var order = repository.Find(command.OrderId);

            if (order != null)
            {
                order.Expire();
                repository.Save(order);
            }
        }

        public void Handle(AssignRegistrantDetails command)
        {
            var order = repository.Find(command.OrderId);

            if (order != null)
            {
                order.AssignRegistrant(command.FirstName, command.LastName, command.Email);
                repository.Save(order);
            }
        }
    }
}

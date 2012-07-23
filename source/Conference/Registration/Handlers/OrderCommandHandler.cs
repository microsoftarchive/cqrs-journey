// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.Handlers
{
    using System.Linq;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging.Handling;
    using Registration.Commands;

    // Note: ConfirmOrderPayment was renamed to this from V1. Make sure there are no commands pending for processing when this is deployed,
    // otherwise the ConfirmOrderPayment commands will not be processed.
    public class OrderCommandHandler :
        ICommandHandler<RegisterToConference>,
        ICommandHandler<MarkSeatsAsReserved>,
        ICommandHandler<RejectOrder>,
        ICommandHandler<AssignRegistrantDetails>,
        ICommandHandler<ConfirmOrder>
    {
        private readonly IEventSourcedRepository<Order> repository;
        private readonly IPricingService pricingService;

        public OrderCommandHandler(IEventSourcedRepository<Order> repository, IPricingService pricingService)
        {
            this.repository = repository;
            this.pricingService = pricingService;
        }

        public void Handle(RegisterToConference command)
        {
            var items = command.Seats.Select(t => new OrderItem(t.SeatType, t.Quantity)).ToList();
            var order = repository.Find(command.OrderId);
            if (order == null)
            {
                order = new Order(command.OrderId, command.ConferenceId, items, pricingService);
            }
            else
            {
                order.UpdateSeats(items, pricingService);
            }

            repository.Save(order, command.Id.ToString());
        }

        public void Handle(MarkSeatsAsReserved command)
        {
            var order = repository.Get(command.OrderId);
            order.MarkAsReserved(this.pricingService, command.Expiration, command.Seats);
            repository.Save(order, command.Id.ToString());
        }

        public void Handle(RejectOrder command)
        {
            var order = repository.Find(command.OrderId);
            // Explicitly idempotent. 
            if (order != null)
            {
                order.Expire();
                repository.Save(order, command.Id.ToString());
            }
        }

        public void Handle(AssignRegistrantDetails command)
        {
            var order = repository.Get(command.OrderId);
            order.AssignRegistrant(command.FirstName, command.LastName, command.Email);
            repository.Save(order, command.Id.ToString());
        }

        public void Handle(ConfirmOrder command)
        {
            var order = repository.Get(command.OrderId);
            order.Confirm();
            repository.Save(order, command.Id.ToString());
        }
    }
}

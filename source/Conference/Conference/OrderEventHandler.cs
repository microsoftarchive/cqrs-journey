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

namespace Conference
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;

    public class OrderEventHandler :
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderRegistrantAssigned>,
        IEventHandler<OrderTotalsCalculated>,
        IEventHandler<OrderPaymentConfirmed>,
        IEventHandler<SeatAssigned>,
        IEventHandler<SeatAssignmentUpdated>,
        IEventHandler<SeatUnassigned>
    {
        private Func<ConferenceContext> contextFactory;

        public OrderEventHandler(Func<ConferenceContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                context.Orders.Add(new Order(@event.ConferenceId, @event.SourceId, @event.AccessCode));
                context.SaveChanges();
            }
        }

        public void Handle(OrderRegistrantAssigned @event)
        {
            ProcessOrder(@event.SourceId, order =>
            {
                order.RegistrantEmail = @event.Email;
                order.RegistrantName = @event.LastName + ", " + @event.FirstName;
            });
        }

        public void Handle(OrderTotalsCalculated @event)
        {
            ProcessOrder(@event.SourceId, order => order.TotalAmount = @event.Total);
        }

        public void Handle(OrderPaymentConfirmed @event)
        {
            ProcessOrder(@event.SourceId, order => order.Status = Order.OrderStatus.Paid);
        }

        public void Handle(SeatAssigned @event)
        {
            ProcessOrder(@event.SourceId, order =>
            {
                var seat = order.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null)
                {
                    seat.Attendee.FirstName = @event.FirstName;
                    seat.Attendee.LastName = @event.LastName;
                    seat.Attendee.Email = @event.Email;
                }
                else
                {
                    order.Seats.Add(new OrderSeat(@event.SourceId, @event.Position, @event.SeatType)
                    {
                        Attendee = new Attendee
                        {
                            FirstName = @event.FirstName,
                            LastName = @event.LastName,
                            Email = @event.Email,
                        }
                    });
                }
            });
        }

        public void Handle(SeatAssignmentUpdated @event)
        {
            ProcessOrder(@event.SourceId, order =>
            {
                var seat = order.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null)
                {
                    seat.Attendee.FirstName = @event.FirstName;
                    seat.Attendee.LastName = @event.LastName;
                }
            });
        }

        public void Handle(SeatUnassigned @event)
        {
            ProcessOrder(@event.SourceId, order =>
            {
                var seat = order.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null)
                {
                    order.Seats.Remove(seat);
                }
            });
        }

        private void ProcessOrder(Guid orderId, Action<Order> orderAction)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var order = context.Orders.Include(x => x.Seats).FirstOrDefault(x => x.Id == orderId);
                if (order != null)
                {
                    orderAction.Invoke(order);
                    context.SaveChanges();
                }
            }
        }
    }
}

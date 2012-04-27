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

namespace SeatAssignment
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;

    public class OrderHandler :
        IEventHandler<OrderReservationCompleted>,
        IEventHandler<OrderPaymentConfirmed>
    {
        private Func<IOrderSeatsDao> dao;

        public OrderHandler(Func<IOrderSeatsDao> dao)
        {
            this.dao = dao;
        }

        public void Handle(OrderReservationCompleted @event)
        {
            var context = this.dao.Invoke();
            using (context as IDisposable)
            {
                var assignments = context.FindOrder(@event.SourceId, false);
                if (assignments == null)
                    assignments = new OrderSeats(@event.SourceId);
                else if (assignments.IsOrderConfirmed)
                    throw new InvalidOperationException("Can't modify reservation for confirmed order " + @event.SourceId);

                assignments.Seats.Clear();
                assignments.Seats.AddRange(@event.Seats.SelectMany(seat =>
                    // Add as many assignments as seats there are.
                    Enumerable
                        .Range(0, seat.Quantity)
                        .Select(i => new Seat { SeatType = seat.SeatType })));

                context.SaveOrder(assignments);
            }
        }

        public void Handle(OrderPaymentConfirmed @event)
        {
            var context = this.dao.Invoke();
            using (context as IDisposable)
            {
                var assignments = context.FindOrder(@event.SourceId, false);
                if (assignments == null)
                    throw new InvalidOperationException("Can't find seat assignments for order " + @event.SourceId);

                assignments.IsOrderConfirmed = true;
                context.SaveOrder(assignments);
            }
        }
    }
}

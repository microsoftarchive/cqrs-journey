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
    using AutoMapper;
    using Infrastructure.Messaging;
    using SeatAssignment.Events;

    public class OrderSeatsService
    {
        private Func<IOrderSeatsDao> dao;
        private IEventBus bus;

        static OrderSeatsService()
        {
            Mapper.CreateMap<Seat, SeatAssignmentAdded>();
            Mapper.CreateMap<Seat, AttendeeUpdated>();
            Mapper.CreateMap<Seat, AttendeeRemoved>();
        }

        public OrderSeatsService(Func<IOrderSeatsDao> dao, IEventBus bus)
        {
            this.dao = dao;
            this.bus = bus;
        }

        public OrderSeats FindOrder(Guid orderId)
        {
            var context = this.dao.Invoke();
            using (context as IDisposable)
            {
                return context.FindOrder(orderId, confirmedOnly: true);
            }
        }

        public void UpdateSeat(Seat seat)
        {
            var context = this.dao.Invoke();
            using (context as IDisposable)
            {
                var events = new List<IEvent>();
                var saved = context.FindSeat(seat.Id);
                if (saved == null)
                    throw new ArgumentException("Seat does not exist.");

                context.UpdateSeat(seat);

                if (saved.Email == null)
                {
                    this.bus.Publish(Mapper.Map(seat, new SeatAssignmentAdded(seat.Id)));
                }
                else if (saved.Email == seat.Email)
                {
                    this.bus.Publish(Mapper.Map(seat, new AttendeeUpdated(seat.Id)));
                }
                else
                {
                    this.bus.Publish(Mapper.Map(saved, new AttendeeRemoved(seat.Id)));
                    this.bus.Publish(Mapper.Map(seat, new SeatAssignmentAdded(seat.Id)));
                }
            }
        }
    }
}

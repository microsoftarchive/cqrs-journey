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
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Registration.Events;
    using Registration.ReadModel;

    public class OrderViewModelGenerator :
        IEventHandler<OrderPlaced>, IEventHandler<OrderUpdated>,
        IEventHandler<OrderPartiallyReserved>, IEventHandler<OrderReservationCompleted>,
        IEventHandler<OrderRegistrantAssigned>
    {
        private Func<IViewRepository> repositoryFactory;

        public OrderViewModelGenerator(Func<IViewRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            var repository = this.repositoryFactory();
            using (repository as IDisposable)
            {
                var dto = new OrderDTO(@event.SourceId, OrderDTO.States.Created)
                {
                    AccessCode = @event.AccessCode,
                };
                dto.Lines.AddRange(@event.Seats.Select(seat => new OrderItemDTO(seat.SeatType, seat.Quantity)));

                repository.Save(dto);
            }
        }

        public void Handle(OrderRegistrantAssigned @event)
        {
            var repository = this.repositoryFactory();
            using (repository as IDisposable)
            {
                var dto = repository.Find<OrderDTO>(@event.SourceId);
                dto.RegistrantEmail = @event.Email;

                repository.Save(dto);
            }
        }

        public void Handle(OrderUpdated @event)
        {
            var repository = this.repositoryFactory();
            using (repository as IDisposable)
            {
                var dto = repository.Find<OrderDTO>(@event.SourceId);
                dto.Lines.Clear();
                dto.Lines.AddRange(@event.Seats.Select(seat => new OrderItemDTO(seat.SeatType, seat.Quantity)));

                repository.Save(dto);
            }
        }

        public void Handle(OrderPartiallyReserved @event)
        {
            this.UpdateReserved(@event.SourceId, @event.ReservationExpiration, OrderDTO.States.PartiallyReserved, @event.Seats);
        }

        public void Handle(OrderReservationCompleted @event)
        {
            this.UpdateReserved(@event.SourceId, @event.ReservationExpiration, OrderDTO.States.ReservationCompleted, @event.Seats);
        }

        private void UpdateReserved(Guid orderId, DateTime reservationExpiration, OrderDTO.States state, IEnumerable<SeatQuantity> seats)
        {
            var repository = this.repositoryFactory();
            using (repository as IDisposable)
            {
                var dto = repository.Find<OrderDTO>(orderId);
                foreach (var seat in seats)
                {
                    var item = dto.Lines.Single(x => x.SeatType == seat.SeatType);
                    item.ReservedSeats = seat.Quantity;
                }

                dto.State = state;
                dto.ReservationExpirationDate = reservationExpiration;

                repository.Save(dto);
            }
        }
    }
}

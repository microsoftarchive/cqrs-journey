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
    using System.IO;
    using System.Linq;
    using System.Text;
    using AutoMapper;
    using Infrastructure.Blob;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Registration.Events;
    using Registration.ReadModel;

    public class SeatAssignmentsViewModelGenerator :
        IEventHandler<SeatAssignmentsCreated>,
        IEventHandler<SeatAssigned>,
        IEventHandler<SeatUnassigned>,
        IEventHandler<SeatAssignmentUpdated>
    {
        private readonly IBlobStorage storage;
        private readonly ITextSerializer serializer;

        public SeatAssignmentsViewModelGenerator(IBlobStorage storage, ITextSerializer serializer)
        {
            this.storage = storage;
            this.serializer = serializer;
        }

        static SeatAssignmentsViewModelGenerator()
        {
            Mapper.CreateMap<SeatAssigned, Seat>();
            Mapper.CreateMap<SeatAssignmentUpdated, Seat>();
        }

        public void Handle(SeatAssignmentsCreated @event)
        {
            var dto = new OrderSeats(@event.SourceId, @event.Seats.Select(i => new Seat(i.Position, i.SeatType)));
            Save(dto);
        }

        public void Handle(SeatAssigned @event)
        {
            var dto = Find(@event.SourceId);
            if (dto != null)
            {
                var seat = dto.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null)
                {
                    Mapper.Map(@event, seat);
                    Save(dto);
                }
            }
        }

        public void Handle(SeatUnassigned @event)
        {
            var dto = Find(@event.SourceId);
            if (dto != null)
            {
                var seat = dto.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null)
                {
                    seat.Email = seat.FirstName = seat.LastName = null;
                    Save(dto);
                }
            }
        }

        public void Handle(SeatAssignmentUpdated @event)
        {
            var dto = Find(@event.SourceId);
            if (dto != null)
            {
                var seat = dto.Seats.FirstOrDefault(x => x.Position == @event.Position);
                if (seat != null)
                {
                    Mapper.Map(@event, seat);
                    Save(dto);
                }
            }
        }

        private OrderSeats Find(Guid id)
        {
            var dto = this.storage.Find("SeatAssignments-" + id);
            if (dto == null)
                return null;

            using (var stream = new MemoryStream(dto))
            using (var reader = new StreamReader(stream))
            {
                return (OrderSeats)this.serializer.Deserialize(reader);
            }
        }

        private void Save(OrderSeats dto)
        {
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, dto);
                this.storage.Save("SeatAssignments-" + dto.Id, "text/plain", Encoding.UTF8.GetBytes(writer.ToString()));
            }
        }
    }
}

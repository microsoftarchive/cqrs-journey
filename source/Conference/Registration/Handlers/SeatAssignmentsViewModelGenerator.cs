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
    using System.Text;
    using Registration.Events;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Blob;
    using Infrastructure.Serialization;
    using Registration.ReadModel;
    using System.IO;

    public class SeatAssignmentsViewModelGenerator : IEventHandler<SeatAssignmentsCreated>
    {
        private IBlobStorage storage;
        private ITextSerializer serializer;

        public SeatAssignmentsViewModelGenerator(IBlobStorage storage, ITextSerializer serializer)
        {
            this.storage = storage;
            this.serializer = serializer;
        }

        public void Handle(SeatAssignmentsCreated @event)
        {
            // Create the whole DTO with one item per seat per type, 
            // so that the UI can easily fill that in.
            var dto = new SeatAssignmentsDTO(@event.SourceId,
                @event.Seats.SelectMany(seat =>
                    // Add as many assignments as seats there are.
                    Enumerable
                        .Range(0, seat.Quantity)
                        .Select(i => new SeatAssignmentDTO(seat.SeatType))));

            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, dto);
                this.storage.Save("SeatAssignments-" + @event.SourceId, "text/plain", Encoding.UTF8.GetBytes(writer.ToString()));
            }
        }
    }
}

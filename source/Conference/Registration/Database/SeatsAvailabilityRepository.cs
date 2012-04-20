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

namespace Registration.Database
{
    using System;
    using Common;

    public sealed class FakeSeatsAvailabilityInitializer
    {
        private readonly IEventSourcedRepository<SeatsAvailability> repository;

        public FakeSeatsAvailabilityInitializer(IEventSourcedRepository<SeatsAvailability> repository)
        {
            this.repository = repository;
        }

        public void Initialize()
        {
            // TODO: remove hardcoded seats availability.
            if (repository.Find(Guid.Empty) == null)
            {
                var availability = new SeatsAvailability(Guid.Empty);
                availability.AddSeats(new Guid("38D8710D-AEF6-4158-950D-3F75CC4BEE0B"), 50);

                repository.Save(availability);
            }
        }
    }
}

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

namespace Registration.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Infrastructure.Messaging;

    public class RegisterToConference : ICommand, IValidatableObject
    {
        public RegisterToConference()
        {
            this.Id = Guid.NewGuid();
            this.Seats = new Collection<SeatQuantity>();
        }

        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public Guid ConferenceId { get; set; }

        public ICollection<SeatQuantity> Seats { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (this.Seats == null || !this.Seats.Any(x => x.Quantity > 0))
            {
                 return new[] { new ValidationResult("One or more items are required.", new[] { "Seats" }) };
            }
            else if (this.Seats.Any(x => x.Quantity < 0))
            {
                return new[] { new ValidationResult("Invalid registration.", new[] { "Seats" }) };
            }

            return Enumerable.Empty<ValidationResult>();
        }
    }
}

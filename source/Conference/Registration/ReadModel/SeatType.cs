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

namespace Registration.ReadModel
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class SeatType
    {
        public SeatType(Guid id, Guid conferenceId, string name, string description, decimal price, int quantity)
        {
            this.Id = id;
            this.ConferenceId = conferenceId;
            this.Name = name;
            this.Description = description;
            this.Price = price;
            this.Quantity = quantity;
            this.AvailableQuantity = 0;
            this.SeatsAvailabilityVersion = -1;
        }

        protected SeatType()
        {
        }

        [Key]
        public Guid Id { get; set; }

        // Conference ID is not FK, as we are relaxing the constraint due to eventual consistency
        public Guid ConferenceId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int SeatsAvailabilityVersion { get; set; }
    }
}

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

namespace Registration.ReadModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;

    public class Conference
    {
        public Conference(Guid id, string code, string name, string description, DateTimeOffset startDate, IEnumerable<SeatType> seats)
        {
            this.Id = id;
            this.Code = code;
            this.Name = name;
            this.Description = description;
            this.StartDate = startDate;
            this.Seats = new ObservableCollection<SeatType>(seats);
        }

        protected Conference()
        {
            this.Seats = new ObservableCollection<SeatType>();
        }

        [Key]
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public ICollection<SeatType> Seats { get; set; }

        public bool IsPublished { get; set; }
    }
}

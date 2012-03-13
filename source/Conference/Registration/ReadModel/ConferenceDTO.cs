// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
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

    public class ConferenceDTO
    {
        public ConferenceDTO(Guid id, string code, string name, string description, IEnumerable<ConferenceSeatDTO> seats)
        {
            this.Id = id;
            this.Code = code;
            this.Name = name;
            this.Description = description;
            this.Seats = new ObservableCollection<ConferenceSeatDTO>(seats);
        }

        protected ConferenceDTO()
        {
            this.Seats = new ObservableCollection<ConferenceSeatDTO>();
        }

        [Key]
        public virtual Guid Id { get; private set; }

        public virtual string Code { get; private set; }

        public virtual string Name { get; private set; }

        public virtual string Description { get; private set; }

        public virtual ObservableCollection<ConferenceSeatDTO> Seats { get; private set; }
    }
}

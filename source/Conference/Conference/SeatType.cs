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

namespace Conference
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Infrastructure.Utils;

    public class SeatType
    {
        public SeatType()
        {
            this.Id = GuidUtil.NewSequentialId();
        }

        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(70, MinimumLength = 2)]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(250)]
        public string Description { get; set; }

        [Range(0, 100000)]
        public int Quantity { get; set; }

        [Range(0, 50000)]
        public decimal Price { get; set; }
    }
}
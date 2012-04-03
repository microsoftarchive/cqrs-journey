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

namespace Registration
{
    using System;

    /// <summary>
    /// Represents a seat reservation.
    /// </summary>
    public class Reservation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Reservation"/> class.
        /// </summary>
        /// <param name="id">The reservation identifier.</param>
        /// <param name="quantity">The number of reserved seats.</param>
        public Reservation(Guid id, int quantity)
            : this()
        {
            this.Id = id;
            this.Quantity = quantity;
        }

        public Guid Id { get; private set; }
        public int Quantity { get; internal set; }

        // ORM requirement
        protected Reservation()
        {
        }
    }
}

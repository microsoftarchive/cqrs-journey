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

namespace Conference
{
    using System;
    using Infrastructure.Messaging;

    /// <summary>
    /// Event raised when seats are added to an existing 
    /// seat type.
    /// </summary>
    public class SeatsAdded : IEvent
    {
        /// <summary>
        /// Gets or sets the conference identifier.
        /// </summary>
        public Guid ConferenceId { get; set; }

        /// <summary>
        /// Gets or sets the source seat type identifier.
        /// </summary>
        public Guid SourceId { get; set; }

        /// <summary>
        /// Gets or sets the total quantity resulting after 
        /// adding the new seats.
        /// </summary>
        public int TotalQuantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity of seats added.
        /// </summary>
        public int AddedQuantity { get; set; }
    }
}

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
    using Infrastructure.Messaging;

    /// <summary>
    /// Event raised when a new seat type is created. Note 
    /// that when a seat type is created.
    /// </summary>
    public class SeatCreated : IEvent
    {
        /// <summary>
        /// Gets or sets the conference identifier.
        /// </summary>
        public Guid ConferenceId { get; set; }

        /// <summary>
        /// Gets or sets the source seat type identifier.
        /// </summary>
        public Guid SourceId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}

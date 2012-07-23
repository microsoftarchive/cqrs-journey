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

namespace Infrastructure.Azure.EventSourcing
{
    using System.Xml.Serialization;

    /// <summary>
    /// Simple settings class to configure the connection to Windows Azure tables.
    /// </summary>
    [XmlRoot("EventSourcing", Namespace = InfrastructureSettings.XmlNamespace)]
    public class EventSourcingSettings
    {
        /// <summary>
        /// Gets or sets the service URI scheme.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the Windows Azure table used for the Orders and Seats Assignments Event Store.
        /// </summary>
        public string OrdersTableName { get; set; }

        /// <summary>
        /// Gets or sets the name of the Windows Azure table used for the Seats Availability Event Store.
        /// </summary>
        public string SeatsAvailabilityTableName { get; set; }
    }
}

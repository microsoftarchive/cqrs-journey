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

namespace Infrastructure.Azure.MessageLog
{
    using Microsoft.WindowsAzure.StorageClient;

    public class MessageLogEntity : TableServiceEntity
    {
        /// <summary>
        /// Gets or sets the kind of entry, Command or Event.
        /// </summary>
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets the message correlation id.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the message id.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The identifier of the object that generated the event.
        /// </summary>
        public string SourceId { get; set; }

        /// <summary>
        /// The simple assembly name of the message payload (i.e. event or command).
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// The namespace of the message payload (i.e. event or command).
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// The full type name of the message payload (i.e. event or command).
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// The simple type name (without the namespace) of the message payload (i.e. event or command).
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// The name of the entity that originated this message.
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// The date and time when this message was created (in Round-trip format)
        /// </summary>
        public string CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the payload of the log.
        /// </summary>
        public string Payload { get; set; }
    }
}
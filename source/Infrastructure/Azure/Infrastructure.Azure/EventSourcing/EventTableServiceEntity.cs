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
    using Microsoft.WindowsAzure.StorageClient;

    public interface IEventRecord
    {
        string PartitionKey { get; }
        string RowKey { get; }
        string SourceId { get; set; }
        string SourceType { get; }
        string Payload { get; }
        string CreationDate { get; }
        string CorrelationId { get; }

        // Standard metadata
        string AssemblyName { get; }
        string Namespace { get; }
        string FullName { get; }
        string TypeName { get; }
    }

    public class EventTableServiceEntity : TableServiceEntity, IEventRecord
    {
        public string SourceId { get; set; }
        public string SourceType { get; set; }
        public string Payload { get; set; }
        public string CreationDate { get; set; }
        public string CorrelationId { get; set; }

        // Standard metadata
        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public string FullName { get; set; }
        public string TypeName { get; set; }
    }
}
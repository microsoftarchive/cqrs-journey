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
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.ServiceBus.Messaging;

    public static class BrokeredMessageExtension
    {
        public static MessageLogEntity ToMessageLogEntity(this BrokeredMessage message)
        {
            var stream = message.GetBody<Stream>();
            var payload = "";
            using (var reader = new StreamReader(stream))
            {
                payload = reader.ReadToEnd();
            }

            return new MessageLogEntity
            {
                PartitionKey = message.EnqueuedTimeUtc.ToString("yyyMM"),
                RowKey = message.EnqueuedTimeUtc.Ticks.ToString("D20") + "_" + message.MessageId,
                MessageId = message.MessageId,
                CorrelationId = message.CorrelationId,
                SourceId = message.Properties.TryGetValue(StandardMetadata.SourceId) as string,
                Kind = message.Properties.TryGetValue(StandardMetadata.Kind) as string,
                AssemblyName = message.Properties.TryGetValue(StandardMetadata.AssemblyName) as string,
                FullName = message.Properties.TryGetValue(StandardMetadata.FullName) as string,
                Namespace = message.Properties.TryGetValue(StandardMetadata.Namespace) as string,
                TypeName = message.Properties.TryGetValue(StandardMetadata.TypeName) as string,
                SourceType = message.Properties.TryGetValue(StandardMetadata.SourceType) as string,
                CreationDate = message.EnqueuedTimeUtc.ToString("o"),
                Payload = payload,
            };
        }
    }
}

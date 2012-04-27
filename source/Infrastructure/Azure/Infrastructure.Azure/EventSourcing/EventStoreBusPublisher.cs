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

namespace Infrastructure.Azure.EventSourcing
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Util;
    using Microsoft.ServiceBus.Messaging;

    public class EventStoreBusPublisher
    {
        private readonly IMessageSender sender;
        private readonly IPendingEventsQueue queue;
        private readonly BlockingCollection<string> enqueuedKeys;
        private static readonly int RowKeyPrefixIndex = "Unpublished".Length;

        public EventStoreBusPublisher(IMessageSender sender, IPendingEventsQueue queue)
        {
            this.sender = sender;
            this.queue = queue;

            this.enqueuedKeys = new BlockingCollection<string>();
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
                                      {
                                          while (!cancellationToken.IsCancellationRequested)
                                          {
                                              string key;
                                              try
                                              {
                                                  key = this.enqueuedKeys.Take(cancellationToken);
                                              }
                                              catch (OperationCanceledException)
                                              {
                                                  return;
                                              }

                                              if (key != null)
                                              {
                                                  try
                                                  {
                                                      var pending = this.queue.GetPending(key).AsCachedAnyEnumerable();
                                                      if (pending.Any())
                                                      {
                                                          foreach (var record in pending)
                                                          {
                                                              this.sender.SendAsync(BuildMessage(record));
                                                          }
                                                      }
                                                  }
                                                  catch
                                                  {
                                                      // if there was ANY unhandled error, re-add the item to collection.
                                                      this.enqueuedKeys.Add(key);
                                                      throw;
                                                  }
                                              }
                                          }
                                      },
                                      TaskCreationOptions.LongRunning);
        }

        public void SendAsync(string partitionKey)
        {
            this.enqueuedKeys.Add(partitionKey);
        }

        private static BrokeredMessage BuildMessage(IEventRecord record)
        {
            return new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(record.Payload)), true)
            {
                MessageId = record.PartitionKey + record.RowKey.Substring(RowKeyPrefixIndex),
                Properties = { { "Kind", record.EventType } }
            };
        }
    }
}

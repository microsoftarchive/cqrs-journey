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

namespace Infrastructure.Azure.Tests.EventSourcing.EventStoreBusPublisherFixture
{
    using System;
    using System.Linq;
    using System.Threading;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.Tests.Mocks;
    using Moq;
    using Xunit;

    public class when_calling_publish
    {
        private Mock<IPendingEventsQueue> queue;
        private MessageSenderMock sender;
        private string partitionKey;
        private string version;
        private IEventRecord testEvent;

        public when_calling_publish()
        {
            this.partitionKey = Guid.NewGuid().ToString();
            this.version = "0001";
            string rowKey = "Unpublished_" + version;
            this.testEvent = Mock.Of<IEventRecord>(x =>
                x.PartitionKey == partitionKey
                && x.RowKey == rowKey
                && x.EventType == "TestEventType"
                && x.SourceId == "TestId"
                && x.SourceType == "TestSourceType"
                && x.Payload == "serialized event");
            this.queue = new Mock<IPendingEventsQueue>();
            queue.Setup(x => x.GetPending(partitionKey)).Returns(new[] { testEvent });
            this.sender = new MessageSenderMock();
            var sut = new EventStoreBusPublisher(sender, queue.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            sut.Start(cancellationTokenSource.Token);

            sut.SendAsync(partitionKey);

            Assert.True(sender.ResetEvent.WaitOne(3000));
            cancellationTokenSource.Cancel();
        }

        [Fact]
        public void then_sends_unpublished_event_with_deterministic_message_id_for_detecting_duplicates()
        {
            string expectedMessageId = string.Format("{0}_{1}", partitionKey, version);
            Assert.Equal(expectedMessageId, sender.Sent.Single().MessageId);
        }

        [Fact]
        public void then_sent_event_contains_friendly_metadata()
        {
            Assert.Equal(testEvent.SourceId, sender.Sent.Single().Properties["SourceId"]);
            Assert.Equal(testEvent.SourceType, sender.Sent.Single().Properties["SourceType"]);
            Assert.Equal(testEvent.EventType, sender.Sent.Single().Properties["EventType"]);
            Assert.Equal(version, sender.Sent.Single().Properties["Version"]);
        }

        [Fact]
        public void then_deletes_message_after_publishing()
        {
            queue.Verify(q => q.DeletePending(partitionKey, testEvent.RowKey));
        }
    }

    public class when_starting_with_pending_events
    {
        private Mock<IPendingEventsQueue> queue;
        private MessageSenderMock sender;
        private string version;
        private string[] pendingKeys;
        private string rowKey;

        public when_starting_with_pending_events()
        {
            this.version = "0001";
            this.rowKey = "Unpublished_" + version;

            this.pendingKeys = new[] { "Key1", "Key2", "Key3" };
            this.queue = new Mock<IPendingEventsQueue>();
            queue.Setup(x => x.GetPending(It.IsAny<string>())).Returns<string>(
                key => new[]
                           {
                               Mock.Of<IEventRecord>(
                                   x => x.PartitionKey == key
                                        && x.RowKey == rowKey
                                        && x.EventType == "TestEventType"
                                        && x.SourceId == "TestId"
                                        && x.SourceType == "TestSourceType"
                                        && x.Payload == "serialized event")
                           });
            queue.Setup(x => x.GetPartitionsWithPendingEvents()).Returns(pendingKeys);
            this.sender = new MessageSenderMock();
            var sut = new EventStoreBusPublisher(sender, queue.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            sut.Start(cancellationTokenSource.Token);

            for (int i = 0; i < pendingKeys.Length; i++)
            {
                Assert.True(sender.ResetEvent.WaitOne(5000));
            }
            cancellationTokenSource.Cancel();
        }

        [Fact]
        public void then_sends_unpublished_event_with_deterministic_message_id_for_detecting_duplicates()
        {
            for (int i = 0; i < pendingKeys.Length; i++)
            {
                string expectedMessageId = string.Format("{0}_{1}", pendingKeys[i], version);
                Assert.Equal(expectedMessageId, sender.Sent.ElementAt(i).MessageId);
            }
        }

        [Fact]
        public void then_sent_event_contains_friendly_metadata()
        {
            for (int i = 0; i < pendingKeys.Length; i++)
            {
                var message = sender.Sent.ElementAt(i);
                Assert.Equal("TestId", message.Properties["SourceId"]);
                Assert.Equal("TestSourceType", message.Properties["SourceType"]);
                Assert.Equal("TestEventType", message.Properties["EventType"]);
                Assert.Equal(version, message.Properties["Version"]);
            }
        }

        [Fact]
        public void then_deletes_message_after_publishing()
        {
            for (int i = 0; i < pendingKeys.Length; i++)
            {
                queue.Verify(q => q.DeletePending(pendingKeys[i], rowKey));
            }
        }
    }
}

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

namespace Infrastructure.Azure.Tests.EventSourcing.Mocks
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.EventSourcing;
    using Microsoft.ServiceBus.Messaging;

    class TestEventComparer : IEqualityComparer<IVersionedEvent>
    {
        public bool Equals(IVersionedEvent x, IVersionedEvent y)
        {
            return x.SourceId == y.SourceId && x.Version == y.Version && ((TestEvent)x).Foo == ((TestEvent)y).Foo;
        }

        public int GetHashCode(IVersionedEvent obj) { throw new NotImplementedException(); }
    }

    class TestEntity : IEventSourced
    {
        public TestEntity()
        {
            this.Events = new List<IVersionedEvent>();
        }

        public TestEntity(Guid id, IEnumerable<IVersionedEvent> history)
        {
            this.Events = new List<IVersionedEvent>();
            this.History = history;
            this.Id = id;
        }

        public IEnumerable<IVersionedEvent> History { get; set; }
        public Guid Id { get; set; }
        public int Version { get; set; }
        public List<IVersionedEvent> Events { get; set; }

        IEnumerable<IVersionedEvent> IEventSourced.Events { get { return this.Events; } }
    }

    class TestEvent : IVersionedEvent
    {
        public Guid SourceId { get; set; }
        public int Version { get; set; }
        public string Foo { get; set; }
    }

    class MessageSenderMock : IMessageSender
    {
        public readonly AutoResetEvent ResetEvent = new AutoResetEvent(false);
        public readonly List<BrokeredMessage> Sent = new List<BrokeredMessage>();

        void IMessageSender.Send(Func<BrokeredMessage> messageFactory)
        {
            this.Sent.Add(messageFactory.Invoke());
            this.ResetEvent.Set();
        }

        void IMessageSender.SendAsync(BrokeredMessage message)
        {
            throw new NotImplementedException();
        }

        void IMessageSender.SendAsync(IEnumerable<BrokeredMessage> messages)
        {
            throw new NotImplementedException();
        }
    }
}

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

namespace Infrastructure.Azure.Tests.EventSourcing.AzureEventSourcedRepositoryFixture
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.Tests.EventSourcing.Mocks;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Moq;
    using Xunit;

    public class when_saving_entity
    {
        private Guid id;
        private Mock<IEventStore> eventStore;
        private Mock<IEventBus> eventBus;

        public when_saving_entity()
        {
            this.eventStore = new Mock<IEventStore>();
            this.eventBus = new Mock<IEventBus>();
            var sut = new AzureEventSourcedRepository<TestEntity>(eventStore.Object, eventBus.Object, new JsonTextSerializer());
            this.id = Guid.NewGuid();
            var entity = new TestEntity
            {
                Id = id,
                Events =
                    {
                        new TestEvent { SourceId = id, Version = 1, Foo = "Bar" },
                        new TestEvent { SourceId = id, Version = 2, Foo = "Baz" }
                    }
            };

            sut.Save(entity);
        }

        [Fact]
        public void then_stores_in_event_store()
        {
            eventStore.Verify(s => s.Save(id.ToString(), It.Is<IEnumerable<EventData>>(x => x.Count() == 2 && x.First().Version == 1 && x.First().Payload.Contains("Bar"))));
        }

        [Fact]
        public void then_publishes_to_bus()
        {
            // TODO: avoid re-serialization, and avoid the EventBus impl altogether
            eventBus.Verify(s => s.Publish(It.Is<IEnumerable<IEvent>>(x => x.Count() == 2 && ((TestEvent)x.First()).Version == 1 && ((TestEvent)x.First()).Foo.Contains("Bar"))));
        }
    }

    public class when_reading_entity
    {
        private Guid id;

        [Fact]
        public void when_reading_entity_then_rehydrates()
        {
            var events = new IVersionedEvent[]
                             {
                                 new TestEvent { SourceId = id, Version = 1, Foo = "Bar" },
                                 new TestEvent { SourceId = id, Version = 2, Foo = "Baz" }                              
                             };
            var serialized = events.Select(x => new EventData { Version = x.Version, Payload = Serialize(x) });
            this.id = Guid.NewGuid();
            var eventStore = new Mock<IEventStore>();
            eventStore.Setup(x => x.Load(It.IsAny<string>(), It.IsAny<int>())).Returns(serialized);
            var sut = new AzureEventSourcedRepository<TestEntity>(eventStore.Object, Mock.Of<IEventBus>(), new JsonTextSerializer());

            var entity = sut.Find(id);

            Assert.NotNull(entity);
            Assert.Equal(id, entity.Id);
            Assert.Equal(events, entity.History, new TestEventComparer());
        }

        private static string Serialize(object graph)
        {
            var serializer = new JsonTextSerializer();
            var writer = new StringWriter();
            serializer.Serialize(writer, graph);
            return writer.ToString();
        }
    }
}

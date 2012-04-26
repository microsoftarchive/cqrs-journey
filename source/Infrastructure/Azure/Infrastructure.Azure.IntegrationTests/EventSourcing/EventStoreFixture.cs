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

namespace Azure.IntegrationTests.EventSourcing
{
    using System;
    using System.Linq;
    using Infrastructure.Azure;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.Messaging;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Xunit;

    public class EventStoreFixture : IDisposable
    {
        private readonly string tableName;
        private CloudStorageAccount account;
        private EventStore sut;

        public EventStoreFixture()
        {
            this.tableName = "EventStoreFixture" + new Random((int)DateTime.Now.Ticks).Next();
            var settings = InfrastructureSettings.ReadEventSourcing("Settings.xml");
            this.account = CloudStorageAccount.Parse(settings.ConnectionString);
            this.sut = new EventStore(this.account, this.tableName);
        }

        [Fact]
        public void can_save_and_load_one_event()
        {
            var id = Guid.NewGuid().ToString();
            var e = new EventData
                {
                    Version = 1,
                    EventType = "Test",
                    Payload = "Payload",
                };

            sut.Save(id, new[] { e });

            var stored = sut.Load(id, 0).ToList();

            Assert.Equal(1, stored.Count);
            Assert.Equal(e.Version, stored[0].Version);
            Assert.Equal(e.EventType, stored[0].EventType);
            Assert.Equal(e.Payload, stored[0].Payload);
        }

        [Fact]
        public void can_load_multiple_events_in_order()
        {
            var id = Guid.NewGuid().ToString();
            var events = new[]
                             {
                                 new EventData { Version = 1, EventType = "Test1", Payload = "Payload1" },
                                 new EventData { Version = 2, EventType = "Test2", Payload = "Payload2" },
                                 new EventData { Version = 3, EventType = "Test3", Payload = "Payload3" },
                             };

            sut.Save(id, events);

            var stored = sut.Load(id, 0).ToList();

            Assert.Equal(3, stored.Count);
            Assert.Equal(1, stored[0].Version);
            Assert.Equal(2, stored[1].Version);
            Assert.Equal(3, stored[2].Version);
            Assert.Equal("Payload1", stored[0].Payload);
            Assert.Equal("Payload2", stored[1].Payload);
            Assert.Equal("Payload3", stored[2].Payload);
        }

        [Fact]
        public void can_load_events_stored_at_different_times()
        {
            var id = Guid.NewGuid().ToString();
            var e1 = new EventData { Version = 1, EventType = "Test1", Payload = "Payload1" };
            var e2 = new EventData { Version = 2, EventType = "Test2", Payload = "Payload2" };
            var e3 = new EventData { Version = 3, EventType = "Test3", Payload = "Payload3" };

            sut.Save(id, new[] { e1, e2 });
            sut.Save(id, new[] { e3 });

            var stored = sut.Load(id, 0).ToList();

            Assert.Equal(3, stored.Count);
            Assert.Equal(1, stored[0].Version);
            Assert.Equal(2, stored[1].Version);
            Assert.Equal(3, stored[2].Version);
            Assert.Equal("Payload1", stored[0].Payload);
            Assert.Equal("Payload2", stored[1].Payload);
            Assert.Equal("Payload3", stored[2].Payload);
        }

        [Fact]
        public void can_load_events_since_specified_version()
        {
            var id = Guid.NewGuid().ToString();
            var events = new[]
                             {
                                 new EventData { Version = 1, EventType = "Test1", Payload = "Payload1" },
                                 new EventData { Version = 2, EventType = "Test2", Payload = "Payload2" },
                                 new EventData { Version = 3, EventType = "Test3", Payload = "Payload3" },
                             };

            sut.Save(id, events);

            var stored = sut.Load(id, 2).ToList();

            Assert.Equal(2, stored.Count);
            Assert.Equal(2, stored[0].Version);
            Assert.Equal(3, stored[1].Version);
            Assert.Equal("Payload2", stored[0].Payload);
            Assert.Equal("Payload3", stored[1].Payload);
        }

        [Fact]
        public void cannot_store_same_version()
        {
            var id = Guid.NewGuid().ToString();
            var e1 = new EventData { Version = 1, EventType = "Test1", Payload = "Payload1" };
            var e2 = new EventData { Version = 1, EventType = "Test2", Payload = "Payload2" };

            sut.Save(id, new[] { e1 });

            Assert.Throws<ConcurrencyException>(() => sut.Save(id, new[] { e2 }));

            var stored = sut.Load(id, 0).ToList();

            Assert.Equal(1, stored.Count);
            Assert.Equal(1, stored[0].Version);
            Assert.Equal("Payload1", stored[0].Payload);
        }

        public void Dispose()
        {
            var client = this.account.CreateCloudTableClient();
            client.DeleteTableIfExist(this.tableName);
        }
    }
}

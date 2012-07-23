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

namespace Infrastructure.Azure.IntegrationTests.AzureEventLogFixture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.MessageLog;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Moq;
    using Xunit;

    public class given_an_empty_event_log : IDisposable
    {
        private readonly string tableName;
        private CloudStorageAccount account;
        protected AzureMessageLogWriter writer;
        protected AzureEventLogReader sut;
        protected string sourceId;
        protected string partitionKey;
        private EventA eventA;
        private EventB eventB;
        private EventC eventC;
        private IMetadataProvider metadata;
        private ITextSerializer serializer;
        private DateTime startEnqueueTime;

        public given_an_empty_event_log()
        {
            this.tableName = "AzureEventLogFixture" + new Random((int)DateTime.Now.Ticks).Next();
            var settings = InfrastructureSettings.Read("Settings.xml").EventSourcing;
            this.account = CloudStorageAccount.Parse(settings.ConnectionString);

            this.eventA = new EventA();
            this.eventB = new EventB();
            this.eventC = new EventC();

            this.metadata = Mock.Of<IMetadataProvider>(x =>
                x.GetMetadata(eventA) == new Dictionary<string, string>
                {
                    { StandardMetadata.SourceId, eventA.SourceId.ToString() },
                    { StandardMetadata.SourceType, "SourceA" }, 
                    { StandardMetadata.Kind, StandardMetadata.EventKind },
                    { StandardMetadata.AssemblyName, "A" }, 
                    { StandardMetadata.Namespace, "Namespace" }, 
                    { StandardMetadata.FullName, "Namespace.EventA" }, 
                    { StandardMetadata.TypeName, "EventA" }, 
                } &&
                x.GetMetadata(eventB) == new Dictionary<string, string>
                {
                    { StandardMetadata.SourceId, eventB.SourceId.ToString() },
                    { StandardMetadata.SourceType, "SourceB" }, 
                    { StandardMetadata.Kind, StandardMetadata.EventKind },
                    { StandardMetadata.AssemblyName, "B" }, 
                    { StandardMetadata.Namespace, "Namespace" }, 
                    { StandardMetadata.FullName, "Namespace.EventB" }, 
                    { StandardMetadata.TypeName, "EventB" }, 
                } &&
                x.GetMetadata(eventC) == new Dictionary<string, string>
                {
                    { StandardMetadata.SourceId, eventC.SourceId.ToString() },
                    { StandardMetadata.SourceType, "SourceC" }, 
                    { StandardMetadata.Kind, StandardMetadata.EventKind },
                    { StandardMetadata.AssemblyName, "B" }, 
                    { StandardMetadata.Namespace, "AnotherNamespace" }, 
                    { StandardMetadata.FullName, "AnotherNamespace.EventC" }, 
                    { StandardMetadata.TypeName, "EventC" }, 
                });

            this.serializer = new JsonTextSerializer();
            this.writer = new AzureMessageLogWriter(this.account, this.tableName);
            this.sut = new AzureEventLogReader(this.account, this.tableName, new JsonTextSerializer());

            this.startEnqueueTime = new DateTime(2012, 06, 30, 23, 59, 0, DateTimeKind.Utc);
            Save(eventA, startEnqueueTime);
            Save(eventB, startEnqueueTime.AddMinutes(5));
            Save(eventC, startEnqueueTime.AddMinutes(6));
        }

        private void Save(IEvent @event, DateTime enqueueTime)
        {
            var message = new MessageLogEntity
            {
                Payload = this.serializer.Serialize(@event),
                PartitionKey = enqueueTime.ToString("yyyMM"),
                RowKey = enqueueTime.Ticks.ToString("D20") + "_" + @event.GetHashCode(),
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString(),
            };

            foreach (var metadata in this.metadata.GetMetadata(@event))
            {
                message.GetType().GetProperty(metadata.Key).SetValue(message, metadata.Value, null);
            }

            this.writer.Save(message);
        }

        public void Dispose()
        {
            var client = this.account.CreateCloudTableClient();
            client.DeleteTableIfExist(this.tableName);
        }

        [Fact]
        public void then_can_read_all()
        {
            var result = this.sut.ReadAll().ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void then_can_filter_by_assembly()
        {
            var events = this.sut.Query(new QueryCriteria { AssemblyNames = { "A" } }).ToList();

            Assert.Equal(1, events.Count);
        }

        [Fact]
        public void then_can_filter_by_multiple_assemblies()
        {
            var events = this.sut.Query(new QueryCriteria { AssemblyNames = { "A", "B" } }).ToList();

            Assert.Equal(3, events.Count);
        }

        [Fact]
        public void then_can_filter_by_namespace()
        {
            var events = this.sut.Query(new QueryCriteria { Namespaces = { "Namespace" } }).ToList();

            Assert.Equal(2, events.Count);
            Assert.True(events.Any(x => x.SourceId == eventA.SourceId));
            Assert.True(events.Any(x => x.SourceId == eventB.SourceId));
        }

        [Fact]
        public void then_can_filter_by_namespaces()
        {
            var events = this.sut.Query(new QueryCriteria { Namespaces = { "Namespace", "AnotherNamespace" } }).ToList();

            Assert.Equal(3, events.Count);
        }

        [Fact]
        public void then_can_filter_by_namespace_and_assembly()
        {
            var events = this.sut.Query(new QueryCriteria { AssemblyNames = { "B" }, Namespaces = { "AnotherNamespace" } }).ToList();

            Assert.Equal(1, events.Count);
            Assert.True(events.Any(x => x.SourceId == eventC.SourceId));
        }

        [Fact]
        public void then_can_filter_by_namespace_and_assembly2()
        {
            var events = this.sut.Query(new QueryCriteria { AssemblyNames = { "A" }, Namespaces = { "AnotherNamespace" } }).ToList();

            Assert.Equal(0, events.Count);
        }

        [Fact]
        public void then_can_filter_by_full_name()
        {
            var events = this.sut.Query(new QueryCriteria { FullNames = { "Namespace.EventA" } }).ToList();

            Assert.Equal(1, events.Count);
            Assert.Equal(eventA.SourceId, events[0].SourceId);
        }

        [Fact]
        public void then_can_filter_by_full_names()
        {
            var events = this.sut.Query(new QueryCriteria { FullNames = { "Namespace.EventA", "AnotherNamespace.EventC" } }).ToList();

            Assert.Equal(2, events.Count);
            Assert.True(events.Any(x => x.SourceId == eventA.SourceId));
            Assert.True(events.Any(x => x.SourceId == eventC.SourceId));
        }

        [Fact]
        public void then_can_filter_by_type_name()
        {
            var events = this.sut.Query(new QueryCriteria { TypeNames = { "EventA" } }).ToList();

            Assert.Equal(1, events.Count);
            Assert.Equal(eventA.SourceId, events[0].SourceId);
        }

        [Fact]
        public void then_can_filter_by_type_names()
        {
            var events = this.sut.Query(new QueryCriteria { TypeNames = { "EventA", "EventC" } }).ToList();

            Assert.Equal(2, events.Count);
            Assert.True(events.Any(x => x.SourceId == eventA.SourceId));
            Assert.True(events.Any(x => x.SourceId == eventC.SourceId));
        }

        [Fact]
        public void then_can_filter_by_type_names_and_assembly()
        {
            var events = this.sut.Query(new QueryCriteria { AssemblyNames = { "B" }, TypeNames = { "EventB", "EventC" } }).ToList();

            Assert.Equal(2, events.Count);
            Assert.True(events.Any(x => x.SourceId == eventB.SourceId));
            Assert.True(events.Any(x => x.SourceId == eventC.SourceId));
        }

        [Fact]
        public void then_can_filter_by_source_id()
        {
            var events = this.sut.Query(new QueryCriteria { SourceIds = { eventA.SourceId.ToString() } }).ToList();

            Assert.Equal(1, events.Count);
            Assert.Equal(eventA.SourceId, events[0].SourceId);
        }

        [Fact]
        public void then_can_filter_by_source_ids()
        {
            var events = this.sut.Query(new QueryCriteria { SourceIds = { eventA.SourceId.ToString(), eventC.SourceId.ToString() } }).ToList();

            Assert.Equal(2, events.Count);
            Assert.True(events.Any(x => x.SourceId == eventA.SourceId));
            Assert.True(events.Any(x => x.SourceId == eventC.SourceId));
        }

        [Fact]
        public void then_can_filter_by_source_type()
        {
            var events = this.sut.Query(new QueryCriteria { SourceTypes = { "SourceA" } }).ToList();

            Assert.Equal(1, events.Count);
        }

        [Fact]
        public void then_can_filter_by_source_types()
        {
            var events = this.sut.Query(new QueryCriteria { SourceTypes = { "SourceA", "SourceB" } }).ToList();

            Assert.Equal(2, events.Count);
        }

        [Fact]
        public void then_can_filter_by_end_date()
        {
            var events = this.sut.Query(new QueryCriteria { EndDate = startEnqueueTime.AddMinutes(5.5) }).ToList();

            Assert.Equal(2, events.Count);
        }

        [Fact]
        public void then_can_use_fluent_criteria_builder()
        {
            var events = this.sut.Query()
                .FromAssembly("A")
                .FromAssembly("B")
                .FromNamespace("Namespace")
                .FromSource("SourceB")
                .WithTypeName("EventB")
                .WithFullName("Namespace.EventB")
                .Until(this.startEnqueueTime.AddMinutes(5))
                .ToList();

            Assert.Equal(1, events.Count);
        }

        public class FakeEvent : IEvent
        {
            public string Value { get; set; }
            public Guid SourceId { get; set; }
        }

        public class EventA : IEvent
        {
            public EventA()
            {
                this.SourceId = Guid.NewGuid();
            }
            public Guid SourceId { get; set; }
        }

        public class EventB : IEvent
        {
            public EventB()
            {
                this.SourceId = Guid.NewGuid();
            }
            public Guid SourceId { get; set; }
        }

        public class EventC : IEvent
        {
            public EventC()
            {
                this.SourceId = Guid.NewGuid();
            }
            public Guid SourceId { get; set; }
        }
    }
}

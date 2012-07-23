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

namespace Infrastructure.Sql.IntegrationTests.SqlEventLogFixture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Infrastructure.MessageLog;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.MessageLog;
    using Moq;
    using Xunit;

    public class given_a_sql_log_with_three_events : IDisposable
    {
        private string dbName = "SqlEventLogFixture_" + Guid.NewGuid().ToString();
        private SqlMessageLog sut;
        private Mock<IMetadataProvider> metadata;
        private EventA eventA;
        private EventB eventB;
        private EventC eventC;

        public given_a_sql_log_with_three_events()
        {
            using (var context = new MessageLogDbContext(dbName))
            {
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                }

                context.Database.Create();
            }

            this.eventA = new EventA();
            this.eventB = new EventB();
            this.eventC = new EventC();

            var metadata = Mock.Of<IMetadataProvider>(x =>
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

            this.metadata = Mock.Get(metadata);
            this.sut = new SqlMessageLog(dbName, new JsonTextSerializer(), metadata);
            this.sut.Save(eventA);
            this.sut.Save(eventB);
            this.sut.Save(eventC);
        }

        public void Dispose()
        {
            using (var context = new MessageLogDbContext(dbName))
            {
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                }
            }
        }

        [Fact]
        public void then_can_read_all()
        {
            var events = this.sut.ReadAll().ToList();

            Assert.Equal(3, events.Count);
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
        public void then_can_filter_in_by_end_date()
        {
            var events = this.sut.Query(new QueryCriteria { EndDate = DateTime.UtcNow }).ToList();

            Assert.Equal(3, events.Count);
        }

        [Fact]
        public void then_can_filter_out_by_end_date()
        {
            var events = this.sut.Query(new QueryCriteria { EndDate = DateTime.UtcNow.AddMinutes(-1) }).ToList();

            Assert.Equal(0, events.Count);
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
                .Until(DateTime.UtcNow)
                .ToList();

            Assert.Equal(1, events.Count);
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

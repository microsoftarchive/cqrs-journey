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

namespace Infrastructure.Sql.EventSourcing
{
    using System;
    using System.Data.Entity;

    /// <summary>
    /// Used by <see cref="SqlEventSourcedRepository{T}"/>, and is used only for running the sample application
    /// without the dependency to the Windows Azure Service Bus when using the DebugLocal solution configuration.
    /// </summary>
    public class EventStoreDbContext : DbContext
    {
        public const string SchemaName = "Events";

        public EventStoreDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Event>().HasKey(x => new { x.AggregateId, x.AggregateType, x.Version }).ToTable("Events", SchemaName);
        }
    }

    public class Event
    {
        public Guid AggregateId { get; set; }
        public string AggregateType { get; set; }
        public int Version { get; set; }
        public string Payload { get; set; }
        public string CorrelationId { get; set; }

        // TODO: Following could be very useful for when rebuilding the read model from the event store, 
        // to avoid replaying every possible event in the system
        // public string EventType { get; set; }
    }
}

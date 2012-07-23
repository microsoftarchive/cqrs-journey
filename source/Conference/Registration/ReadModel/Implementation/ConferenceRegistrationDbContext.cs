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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
    using Microsoft.Practices.TransientFaultHandling;

    /// <summary>
    /// A repository stored in a database for the views.
    /// </summary>
    public class ConferenceRegistrationDbContext : DbContext
    {
        public const string SchemaName = "ConferenceRegistration";
        private readonly RetryPolicy<SqlAzureTransientErrorDetectionStrategy> retryPolicy;

        public ConferenceRegistrationDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            this.retryPolicy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(new Incremental(3, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1.5)) { FastFirstRetry = true });
            this.retryPolicy.Retrying += (s, e) =>
                Trace.TraceWarning("An error occurred in attempt number {1} to access the ConferenceRegistrationDbContext: {0}", e.LastException.Message, e.CurrentRetryCount);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make the name of the views match exactly the name of the corresponding property.
            modelBuilder.Entity<DraftOrder>().ToTable("OrdersViewV3", SchemaName);
            modelBuilder.Entity<DraftOrder>().HasMany(c => c.Lines).WithRequired();
            modelBuilder.Entity<DraftOrderItem>().ToTable("OrderItemsViewV3", SchemaName);
            modelBuilder.Entity<DraftOrderItem>().HasKey(item => new { item.OrderId, item.SeatType });
            modelBuilder.Entity<PricedOrder>().ToTable("PricedOrdersV3", SchemaName);
            modelBuilder.Entity<PricedOrder>().HasMany(c => c.Lines).WithRequired().HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<PricedOrderLine>().ToTable("PricedOrderLinesV3", SchemaName);
            modelBuilder.Entity<PricedOrderLine>().HasKey(seat => new { seat.OrderId, seat.Position });
            modelBuilder.Entity<PricedOrderLineSeatTypeDescription>().ToTable("PricedOrderLineSeatTypeDescriptionsV3", SchemaName);

            modelBuilder.Entity<Conference>().ToTable("ConferencesView", SchemaName);
            modelBuilder.Entity<SeatType>().ToTable("ConferenceSeatTypesView", SchemaName);
        }

        public T Find<T>(Guid id) where T : class
        {
            return this.retryPolicy.ExecuteAction(() => this.Set<T>().Find(id));
        }

        public IQueryable<T> Query<T>() where T : class
        {
            return this.Set<T>();
        }

        public void Save<T>(T entity) where T : class
        {
            var entry = this.Entry(entity);

            if (entry.State == System.Data.EntityState.Detached)
                this.Set<T>().Add(entity);

            this.retryPolicy.ExecuteAction(() => this.SaveChanges());
        }
    }
}

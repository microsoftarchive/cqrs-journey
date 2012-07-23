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

namespace RegistrationV2.ReadModel.Implementation
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    /// <summary>
    /// A repository stored in a database for the views.
    /// </summary>
    public class ConferenceRegistrationDbContext : DbContext
    {
        public const string SchemaName = "ConferenceRegistration";

        public ConferenceRegistrationDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make the name of the views match exactly the name of the corresponding property.
            modelBuilder.Entity<DraftOrder>().ToTable("OrdersView", SchemaName);
            modelBuilder.Entity<DraftOrder>().HasMany(c => c.Lines).WithRequired();
            modelBuilder.Entity<DraftOrderItem>().ToTable("OrderItemsView", SchemaName);
            modelBuilder.Entity<PricedOrder>().ToTable("PricedOrders", SchemaName);
            modelBuilder.Entity<PricedOrder>().HasMany(c => c.Lines).WithRequired().HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<PricedOrderLine>().ToTable("PricedOrderLines", SchemaName);
            modelBuilder.Entity<PricedOrderLineSeatTypeDescription>().ToTable("PricedOrderLineSeatTypeDescriptions", SchemaName);
        }

        public T Find<T>(Guid id) where T : class
        {
            return this.Set<T>().Find(id);
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

            this.SaveChanges();
        }
    }
}

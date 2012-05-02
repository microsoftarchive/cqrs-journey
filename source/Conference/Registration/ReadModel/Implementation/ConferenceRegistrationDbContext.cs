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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Data.Entity.Infrastructure;

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
            modelBuilder.Entity<OrderDTO>().ToTable("OrdersView", SchemaName);
            modelBuilder.Entity<OrderDTO>().HasMany(c => c.Lines).WithRequired().Map(c => c.MapKey("OrderDTO_OrderId"));
            modelBuilder.Entity<OrderItemDTO>().ToTable("OrderItemsView", SchemaName);
            modelBuilder.Entity<TotalledOrder>().ToTable("TotalledOrders", SchemaName);
            modelBuilder.Entity<TotalledOrder>().HasMany(c => c.Lines).WithRequired().HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<TotalledOrderLine>().ToTable("TotalledOrderLines", SchemaName);

            modelBuilder.Entity<ConferenceDTO>().ToTable("ConferencesView", SchemaName);
            modelBuilder.Entity<ConferenceDTO>().HasMany(c => c.Seats).WithRequired().Map(c => c.MapKey("ConferencesView_Id"));
            modelBuilder.Entity<ConferenceSeatTypeDTO>().ToTable("ConferenceSeatsView", SchemaName);
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

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

namespace Registration.ReadModel
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using Common;

    /// <summary>
    /// A repository stored in a database for the views.
    /// </summary>
    public class OrmViewRepository : DbContext, IViewRepository
    {
        public OrmViewRepository()
            // NOTE: by default, we point to the same database 
            // as the aggregates because we're using SQL views, 
            // but of course it could be a separate one.
            : this("ConferenceRegistration")
        {
        }

        public OrmViewRepository(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make the name of the views match exactly the name of the corresponding property.
            modelBuilder.Entity<OrderDTO>().ToTable("OrdersView");
            modelBuilder.Entity<OrderDTO>().HasMany(c => c.Lines).WithRequired().Map(c => c.MapKey("OrdersView_Id"));
            modelBuilder.Entity<OrderLineDTO>().ToTable("OrderLinesView");
            modelBuilder.Entity<ConferenceDTO>().ToTable("ConferencesView");
            modelBuilder.Entity<ConferenceDTO>().HasMany(c => c.Seats).WithRequired().Map(c => c.MapKey("ConferencesView_Id"));
            modelBuilder.Entity<ConferenceSeatDTO>().ToTable("ConferenceSeatsView");
        }

        public T Find<T>(Guid id) where T : class
        {
            return this.Set<T>().Find(id);
        }

        public IQueryable<T> Query<T>() where T : class
        {
            return this.Set<T>();
        }
    }
}

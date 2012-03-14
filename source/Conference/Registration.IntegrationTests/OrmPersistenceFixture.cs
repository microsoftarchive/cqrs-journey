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

namespace Registration.IntegrationTests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data.Entity;
    using System.Linq;
    using EntityPersistence;
    using Xunit;

    /// <summary>
    /// Tests miscelaneous behaviors of EF.
    /// </summary>
    public class OrmPersistenceFixture
    {
        [Fact]
        public void WhenSavingGraph_ThenPersistsEnumerableCollection()
        {
            using (var context = new TestOrm())
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            using (var context = new TestOrm())
            {
                var order = new Order(new Item(new Product()), new Item(new Product()));
                context.Save(order);
            }

            using (var context = new TestOrm())
            {
                var order = context.Query<Order>().First();

                Assert.Equal(2, order.Items.Count);
                Assert.NotNull(order.Items[0].Product);
                Assert.NotNull(order.Items[1].Product);
            }

        }


        public class TestOrm : DbContext
        {
            public void Save<T>(T entity)
                where T : class
            {
                this.Set<T>().Add(entity);
                this.SaveChanges();
            }

            public IQueryable<T> Query<T>()
                where T : class
            {
                return this.Set<T>();
            }

            public virtual DbSet<Order> Orders { get; set; }
        }
    }

    namespace EntityPersistence
    {
        public class Order
        {
            protected Order() { }

            public Order(params Item[] items)
            {
                this.Id = Guid.NewGuid();
                this.Items = new ObservableCollection<Item>();
                foreach (var item in items)
                {
                    this.Items.Add(item);
                }
            }

            public virtual Guid Id { get; private set; }
            public virtual ObservableCollection<Item> Items { get; private set; }
        }

        public class Item
        {
            protected Item() { }

            public Item(Product product)
            {
                this.Id = Guid.NewGuid();
                this.Product = product;
            }

            public virtual Guid Id { get; private set; }
            public virtual Product Product { get; private set; }
        }

        public class Product
        {
            public Product()
            {
                this.Id = Guid.NewGuid();
            }

            public virtual Guid Id { get; set; }
        }
    }
}

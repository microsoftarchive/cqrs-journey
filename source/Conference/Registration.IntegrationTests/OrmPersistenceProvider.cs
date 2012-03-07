// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.IntegrationTests
{
    using System.Data.Entity;
    using Common;
    using Registration.Database;
    using Registration.Tests;

    public class OrmPersistenceProvider : IPersistenceProvider
    {
        private OrmRepository orm;

        private OrmRepository Orm
        {
            get
            {
                if (this.orm == null)
                {
                    using (var context = new OrmRepository("TestOrmRepository"))
                    {
                        if (context.Database.Exists())
                            context.Database.Delete();

                        System.Data.Entity.Database.SetInitializer(new OrmRepositoryInitializer(new DropCreateDatabaseAlways<OrmRepository>()));
                        context.Database.Initialize(true);
                    }

                    this.orm = new OrmRepository("TestOrmRepository");
                }

                return this.orm;
            }
        }

        public T PersistReload<T>(T sut)
            where T : class, IAggregateRoot
        {
            this.Orm.Save(sut);
            this.Orm.Entry(sut).Reload();
            return sut;
        }

        public void Dispose()
        {
            if (this.orm != null)
                this.orm.Dispose();
        }
    }
}

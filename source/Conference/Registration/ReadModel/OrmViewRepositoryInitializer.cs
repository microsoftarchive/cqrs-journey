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
    using System.Data.Entity.Infrastructure;
    using System.Data.SqlClient;
    using System.Linq;
    using Registration.Database;

    public class OrmViewRepositoryInitializer : IDatabaseInitializer<OrmRepository>
    {
        // NOTE: we initialize the same OrmRepository for both because we happen to 
        // persist the views in the same database. This is not required and could be 
        // a separate one if we weren't using SQL Views to drive them.
        private IDatabaseInitializer<OrmRepository> innerInitializer;

        public OrmViewRepositoryInitializer(IDatabaseInitializer<OrmRepository> innerInitializer)
        {
            this.innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(OrmRepository context)
        {
            this.innerInitializer.InitializeDatabase(context);

            using (var viewContext = new OrmViewRepository())
            {
                var ddl = ((IObjectContextAdapter)viewContext).ObjectContext.CreateDatabaseScript();
                try
                {
                    // Initializer will be called even if the DB already exists, 
                    // Once in the app domain.
                    context.Database.ExecuteSqlCommand(ddl);

                    // TODO: remove hardcoded conference

                    var description =
        @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras sit amet ultrices erat. Aenean lacus mi, placerat a ullamcorper ornare, dapibus quis odio. Integer sed tempor purus. Quisque fermentum egestas lobortis. Vivamus nibh felis, sagittis et iaculis et, porta id diam. Aliquam erat volutpat. Nunc a lectus velit, id luctus massa. Maecenas feugiat lectus eu purus semper at tincidunt tortor tristique. Suspendisse adipiscing, nisl ac gravida tempor, tellus massa condimentum ipsum, eget tristique tortor tortor ut lorem. Nam ut ipsum mauris, a hendrerit felis. Sed fermentum orci eget purus pharetra pharetra. Curabitur elementum, eros eu cursus placerat, ante felis iaculis leo, et vehicula odio eros sit amet nisi. Nunc sagittis turpis in sem tincidunt quis malesuada nulla dignissim.

Sed ac nibh mauris. Curabitur et purus odio, vitae iaculis augue. Donec scelerisque dolor sit amet purus volutpat in bibendum massa imperdiet. Fusce mattis sapien id sapien vehicula sodales. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Suspendisse sem tellus, rhoncus sed scelerisque eget, pellentesque in nibh. Mauris suscipit tristique mattis. Quisque consequat, velit tempor laoreet fringilla, nunc erat lacinia orci, in convallis lectus diam vitae augue. Maecenas rhoncus bibendum mi at malesuada. Quisque ut purus odio, a facilisis lectus. Nulla facilisis venenatis suscipit. Proin egestas lectus vel diam volutpat tempor.

Quisque pellentesque, est volutpat viverra tristique, erat enim tincidunt risus, vel consectetur nulla quam et justo. Ut nec condimentum felis. Vivamus bibendum risus ut nibh scelerisque eget sodales purus tincidunt. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse non libero ante. Mauris felis dolor, aliquam vitae luctus vel, elementum in mauris. Donec a risus purus. Fusce sit amet lobortis velit. Nam lacinia sagittis fermentum. Nulla sapien erat, cursus a porta non, malesuada ut erat. Vivamus pharetra erat eu metus varius vel placerat nunc interdum. Sed tristique, risus eu sollicitudin aliquam, nibh purus rhoncus dolor, in elementum arcu orci eu lorem. Cras a diam mattis nisl laoreet tempus quis in nunc. Aliquam erat volutpat.";

                    context.Database
                        .ExecuteSqlCommand(
                            @"INSERT INTO [dbo].[ConferencesView] (Id, Code, Name, Description) VALUES (@Id, @Code, @Name, @Description)",
                            new SqlParameter("Id", Guid.Empty),
                            new SqlParameter("Code", "pandpsymposium"),
                            new SqlParameter("Name", "P&P Symposium"),
                            new SqlParameter("Description", description));

                    context.Database
                        .ExecuteSqlCommand(
                            @"INSERT INTO [dbo].[ConferenceSeatsView] (Id, Description, Price, ConferencesView_Id) VALUES (@Id, @Description, @Price, @ConferencesView_Id)",
                            new SqlParameter("Id", new Guid("38D8710D-AEF6-4158-950D-3F75CC4BEE0B")),
                            new SqlParameter("Description", "Test seat"),
                            new SqlParameter("Price", 10d),
                            new SqlParameter("ConferencesView_Id", Guid.Empty));
                }
                catch (SqlException se)
                {
                    if (se.Class != 16)
                        throw;
                }
            }

            // TODO: EF is creating this entity as a table. Avoid that from the begining instead of dropping it after the fact.
            if (context.Database.SqlQuery<int>("SELECT object_id FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ConferenceAliasesView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"DROP TABLE [dbo].[ConferenceAliasesView]");
            }

            if (!context.Database.SqlQuery<int>("SELECT object_id FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ConferenceAliasesView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"
CREATE VIEW [dbo].[ConferenceAliasesView]
AS
SELECT     
dbo.ConferencesView.Id AS Id, 
dbo.ConferencesView.Code as Code,
dbo.ConferencesView.Name as Name
FROM dbo.ConferencesView");
            }

            if (context.Database.SqlQuery<int>("SELECT object_id FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ConferenceDescriptionsView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"DROP TABLE [dbo].[ConferenceDescriptionsView]");
            }

            if (!context.Database.SqlQuery<int>("SELECT object_id FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ConferenceDescriptionsView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"
CREATE VIEW [dbo].[ConferenceDescriptionsView]
AS
SELECT     
dbo.ConferencesView.Id AS Id, 
dbo.ConferencesView.Code as Code,
dbo.ConferencesView.Name as Name,
dbo.ConferencesView.Description as Description
FROM dbo.ConferencesView");
            }

            context.SaveChanges();
        }
    }
}

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

namespace Registration.Database
{
    using System;
    using System.Data.Entity;
    using System.Data.SqlClient;
    using System.Linq;

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

            if (!context.Database.SqlQuery<string>("SELECT object_id FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[OrdersView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"
CREATE VIEW [dbo].[OrdersView]
AS
SELECT     
    dbo.Orders.Id AS OrderId, 
    dbo.Orders.State as StateValue
FROM dbo.Orders");
            }

            if (!context.Database.SqlQuery<string>("SELECT object_id FROM sys.tables WHERE object_id = OBJECT_ID(N'[dbo].[ConferencesView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"
CREATE TABLE [dbo].[ConferencesView]
(
        Id uniqueidentifier NOT NULL,
        Code nvarchar(255) NOT NULL,
        Name nvarchar(255) NOT NULL,
        Description nvarchar(4000) NULL,
        PRIMARY KEY (Id)
)");

                context.Database.ExecuteSqlCommand(@"
CREATE TABLE [dbo].[ConferenceSeatsView]
(
        Id uniqueidentifier NOT NULL,
        Description nvarchar(255) NOT NULL,
        Price float NOT NULL,
        ConferencesView_Id uniqueidentifier NOT NULL,
        PRIMARY KEY (Id),
        FOREIGN KEY (ConferencesView_Id) REFERENCES [dbo].[ConferencesView](Id)
)");

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
                        new SqlParameter("Id", Guid.NewGuid()),
                        new SqlParameter("Description", "Test seat"),
                        new SqlParameter("Price", 10d),
                        new SqlParameter("ConferencesView_Id", Guid.Empty));
            }

            context.SaveChanges();

        }
    }
}

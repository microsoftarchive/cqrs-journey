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

namespace Registration.Database
{
    using System.Data.Entity;
    using System.Linq;

    public class RegistrationProcessManagerDbContextInitializer : IDatabaseInitializer<RegistrationProcessManagerDbContext>
    {
        private IDatabaseInitializer<RegistrationProcessManagerDbContext> innerInitializer;

        public RegistrationProcessManagerDbContextInitializer(IDatabaseInitializer<RegistrationProcessManagerDbContext> innerInitializer)
        {
            this.innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(RegistrationProcessManagerDbContext context)
        {
            this.innerInitializer.InitializeDatabase(context);

            CreateIndexes(context);

            context.SaveChanges();
        }

        public static void CreateIndexes(DbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_RegistrationProcessManager_Completed')
CREATE NONCLUSTERED INDEX IX_RegistrationProcessManager_Completed ON [" + RegistrationProcessManagerDbContext.SchemaName + @"].[RegistrationProcess]( Completed )
            
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_RegistrationProcessManager_OrderId')
CREATE NONCLUSTERED INDEX IX_RegistrationProcessManager_OrderId ON [" + RegistrationProcessManagerDbContext.SchemaName + @"].[RegistrationProcess]( OrderId )");

//IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_RegistrationProcessManager_ReservationId')
//CREATE NONCLUSTERED INDEX IX_RegistrationProcessManager_ReservationId ON [" + RegistrationProcessDbContext.SchemaName + @"].[RegistrationProcess]( ReservationId )
        }
    }
}

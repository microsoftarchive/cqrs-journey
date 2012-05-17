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

namespace MigrationToV2
{
    using System.Data.Entity;
    using Registration.ReadModel;

    public class ConferenceRegistrationMigrationDbContext : MigrationDbContext
    {
        public const string SchemaName = "ConferenceRegistration";
        private const string MigrationSchemaName = "MigrationV1";

        public ConferenceRegistrationMigrationDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Conference>().ToTable("ConferencesView", SchemaName);
            modelBuilder.Entity<Conference>().HasMany(c => c.Seats).WithRequired();
            modelBuilder.Entity<SeatType>().ToTable("ConferenceSeatTypesView", SchemaName);
        }

        public override void UpdateTables()
        {
            this.CreateSchema(MigrationSchemaName);

            this.TransferObject("ConferencesView", SchemaName, MigrationSchemaName);
            this.TransferObject("ConferenceSeatTypesView", SchemaName, MigrationSchemaName);

            this.CreateTables();
        }

        public override void RollbackTablesMigration()
        {
            this.DropTable("ConferenceSeatTypesView", SchemaName);
            this.DropTable("ConferencesView", SchemaName);

            this.TransferObject("ConferencesView", MigrationSchemaName, SchemaName);
            this.TransferObject("ConferenceSeatTypesView", MigrationSchemaName, SchemaName);
        }
    }
}

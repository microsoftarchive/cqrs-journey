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

namespace MigrationToV2
{
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Globalization;

    public abstract class MigrationDbContext : DbContext
    {
        private const string CreateMigrationSchemaCommand = @"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{0}') EXECUTE sp_executesql N'CREATE SCHEMA [{0}] AUTHORIZATION [dbo]';";
        private const string TransferCommand = @"ALTER SCHEMA {2} TRANSFER {1}.{0}";
        private const string DropTableCommand = @"DROP TABLE {1}.{0}";

        public MigrationDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public abstract void UpdateTables();

        public abstract void RollbackTablesMigration();

        protected void CreateTables()
        {
            var adapter = (IObjectContextAdapter)this;
            var script = adapter.ObjectContext.CreateDatabaseScript();
            this.Database.ExecuteSqlCommand(script);
        }

        protected void CreateSchema(string schemaName)
        {
            this.Database.ExecuteSqlCommand(string.Format(CultureInfo.InvariantCulture, CreateMigrationSchemaCommand, schemaName));
        }

        protected void TransferObject(string objectName, string currentSchema, string newSchema)
        {
            this.Database.ExecuteSqlCommand(string.Format(CultureInfo.InvariantCulture, TransferCommand, objectName, currentSchema, newSchema));
        }

        protected void DropTable(string tableName, string schemaName)
        {
            this.Database.ExecuteSqlCommand(string.Format(CultureInfo.InvariantCulture, DropTableCommand, tableName, schemaName));
        }
    }
}

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

namespace Registration.ReadModel.Implementation
{
    using System.Data.Entity;

    public class ConferenceRegistrationDbContextInitializer : IDatabaseInitializer<ConferenceRegistrationDbContext>
    {
        private IDatabaseInitializer<ConferenceRegistrationDbContext> innerInitializer;

        public ConferenceRegistrationDbContextInitializer(IDatabaseInitializer<ConferenceRegistrationDbContext> innerInitializer)
        {
            this.innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(ConferenceRegistrationDbContext context)
        {
            this.innerInitializer.InitializeDatabase(context);

            CreateIndexes(context);

            context.SaveChanges();
        }

        public static void CreateIndexes(DbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = 'IX_SeatTypesView_ConferenceId')
CREATE NONCLUSTERED INDEX IX_SeatTypesView_ConferenceId ON [" + ConferenceRegistrationDbContext.SchemaName + "].[ConferenceSeatTypesView]( ConferenceId )");
        }
    }
}

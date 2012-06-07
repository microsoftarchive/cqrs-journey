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

namespace MigrationToV3
{
    using System.Data.Entity;
    using System.Data.SqlClient;
    using Registration.ReadModel.Implementation;

    /// <summary>
    /// This initializer automatically updates the table for PricedOrders and adds 
    /// the new ReservationExpirationDate column.
    /// Database initializers in Entity Framework run only once per AppDomain, so this code 
    /// has very little impact even if it continues to be run on subsequent releases. 
    /// It is kept in a separate project so that further releases can easily detect what
    /// code paths aren't needed to run anymore.
    /// This also means it's a no-downtime migration.
    /// </summary>
    internal class ConferenceRegistrationDbInitializer : IDatabaseInitializer<ConferenceRegistrationDbContext>
    {
        public void InitializeDatabase(ConferenceRegistrationDbContext context)
        {
            try
            {
                // Note that we only add the column if it doesn't exist already, so this 
                // can safely run with already upgraded databases.
                context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'ConferenceRegistration' AND TABLE_NAME = 'PricedOrders' AND COLUMN_NAME = 'ReservationExpirationDate')
ALTER TABLE [ConferenceRegistration].[PricedOrders]
ADD [ReservationExpirationDate] [datetime] NULL");
            }
            catch (SqlException e)
            {
                if (e.Number != 2705)
                {
                    throw;
                }
            }
        }
    }
}

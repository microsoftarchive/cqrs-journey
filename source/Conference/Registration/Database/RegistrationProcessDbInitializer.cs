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

namespace Registration.Database
{
    using System.Data.Entity;

    public class RegistrationProcessDbInitializer : IDatabaseInitializer<RegistrationProcessDbContext>
    {
        public void InitializeDatabase(RegistrationProcessDbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[ConferenceRegistrationProcesses].[UndispatchedMessages]') AND type in (N'U'))
CREATE TABLE [ConferenceRegistrationProcesses].[UndispatchedMessages](
	[Id] [uniqueidentifier] NOT NULL,
	[Commands] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
");
        }
    }
}
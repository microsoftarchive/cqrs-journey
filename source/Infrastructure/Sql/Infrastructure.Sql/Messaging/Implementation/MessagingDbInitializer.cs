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

namespace Infrastructure.Sql.Messaging.Implementation
{
    using System.Data.SqlClient;
    using System.Globalization;

    /// <summary>
    /// This database initializer is to support <see cref="CommandBus"/> and <see cref="EventBus"/>, which should be only
    /// used for running the sample application without the dependency to the Windows Azure Service Bus when using the
    /// DebugLocal solution configuration. It should not be used in production systems.
    /// </summary>
    public class MessagingDbInitializer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification="Does not contain user input.")]
        public static void CreateDatabaseObjects(string connectionString, string schema, bool createDatabase = false)
        {
            if (createDatabase)
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = builder.InitialCatalog;
                builder.InitialCatalog = "master";
                builder.AttachDBFilename = string.Empty;

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText =
                            string.Format(
                                CultureInfo.InvariantCulture,
                                @"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{0}') CREATE DATABASE [{0}];",
                                databaseName);

                        command.ExecuteNonQuery();
                    }
                }
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{0}')
EXECUTE sp_executesql N'CREATE SCHEMA [{0}] AUTHORIZATION [dbo]';
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[Commands]') AND type in (N'U'))
CREATE TABLE [{0}].[Commands](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [Body] [nvarchar](max) NOT NULL,
    [DeliveryDate] [datetime] NULL,
    [CorrelationId] [nvarchar](max) NULL,
 CONSTRAINT [PK_{0}.Commands] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY];
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[Events]') AND type in (N'U'))
CREATE TABLE [{0}].[Events](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [Body] [nvarchar](max) NOT NULL,
    [DeliveryDate] [datetime] NULL,
    [CorrelationId] [nvarchar](max) NULL,
 CONSTRAINT [PK_{0}.Events] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY];
",
                            schema);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

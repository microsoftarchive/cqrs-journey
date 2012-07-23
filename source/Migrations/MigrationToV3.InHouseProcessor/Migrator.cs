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

namespace MigrationToV3.InHouseProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.SqlClient;
    using System.Linq;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.BlobStorage;
    using Infrastructure.MessageLog;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Registration.Handlers;
    using Registration.ReadModel.Implementation;

    /// <summary>
    /// Supports the migration of the app deployed to pseudo-production from V2 to V3.
    /// </summary>
    public class Migrator
    {
        public void CreateV3ReadModelSubscriptions(ServiceBusSettings serviceBusSettings)
        {
            var commandsTopic = serviceBusSettings.Topics.First(t => !t.IsEventBus);
            serviceBusSettings.Topics.Remove(commandsTopic);

            var eventsTopic = serviceBusSettings.Topics.First();
            eventsTopic.MigrationSupport.Clear();
            var v3Subs = eventsTopic.Subscriptions.Where(s => s.Name.EndsWith("V3")).ToArray();
            eventsTopic.Subscriptions.Clear();
            eventsTopic.Subscriptions.AddRange(v3Subs);

            var config = new ServiceBusConfig(serviceBusSettings);
            config.Initialize();
        }

        public void CreateV3ReadModelTables(string dbConnectionString)
        {
            using (var connection = new SqlConnection(dbConnectionString))
            {
                connection.Open();

                var createTables = @"
EXEC sp_rename '[ConferenceRegistration].[DraftOrder_Lines]', 'DraftOrder_LinesV2', 'OBJECT'
EXEC sp_rename '[ConferenceRegistration].[PricedOrder_Lines]', 'PricedOrder_LinesV2', 'OBJECT'

CREATE TABLE [ConferenceRegistration].[PricedOrderLineSeatTypeDescriptionsV3](
    [SeatTypeId] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
    [SeatTypeId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)

CREATE TABLE [ConferenceRegistration].[OrdersViewV3](
    [OrderId] [uniqueidentifier] NOT NULL,
    [ConferenceId] [uniqueidentifier] NOT NULL,
    [ReservationExpirationDate] [datetime] NULL,
    [StateValue] [int] NOT NULL,
    [OrderVersion] [int] NOT NULL,
    [RegistrantEmail] [nvarchar](max) NULL,
    [AccessCode] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
    [OrderId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)

CREATE TABLE [ConferenceRegistration].[PricedOrdersV3](
    [OrderId] [uniqueidentifier] NOT NULL,
    [AssignmentsId] [uniqueidentifier] NULL,
    [Total] [decimal](18, 2) NOT NULL,
    [OrderVersion] [int] NOT NULL,
    [IsFreeOfCharge] [bit] NOT NULL,
    [ReservationExpirationDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
    [OrderId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)

CREATE TABLE [ConferenceRegistration].[OrderItemsViewV3](
    [OrderId] [uniqueidentifier] NOT NULL,
    [SeatType] [uniqueidentifier] NOT NULL,
    [RequestedSeats] [int] NOT NULL,
    [ReservedSeats] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
    [OrderId] ASC,
    [SeatType] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)

CREATE TABLE [ConferenceRegistration].[PricedOrderLinesV3](
    [OrderId] [uniqueidentifier] NOT NULL,
    [Position] [int] NOT NULL,
    [Description] [nvarchar](max) NULL,
    [UnitPrice] [decimal](18, 2) NOT NULL,
    [Quantity] [int] NOT NULL,
    [LineTotal] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
    [OrderId] ASC,
    [Position] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
ALTER TABLE [ConferenceRegistration].[OrderItemsViewV3]  WITH CHECK ADD  CONSTRAINT [DraftOrder_Lines] FOREIGN KEY([OrderId])
REFERENCES [ConferenceRegistration].[OrdersViewV3] ([OrderId])
ON DELETE CASCADE
ALTER TABLE [ConferenceRegistration].[OrderItemsViewV3] CHECK CONSTRAINT [DraftOrder_Lines]

ALTER TABLE [ConferenceRegistration].[PricedOrderLinesV3]  WITH CHECK ADD  CONSTRAINT [PricedOrder_Lines] FOREIGN KEY([OrderId])
REFERENCES [ConferenceRegistration].[PricedOrdersV3] ([OrderId])
ON DELETE CASCADE
ALTER TABLE [ConferenceRegistration].[PricedOrderLinesV3] CHECK CONSTRAINT [PricedOrder_Lines]
";

                var command = new SqlCommand(createTables, connection);

                command.ExecuteNonQuery();
            }
        }

        public void RegenerateV3ViewModels(AzureEventLogReader logReader, IBlobStorage blobStorage, string dbConnectionString, DateTime maxEventTime)
        {
            Database.SetInitializer<ConferenceRegistrationDbContext>(null);

            var handlers = new List<IEventHandler>();
            handlers.Add(new DraftOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(dbConnectionString)));
            handlers.Add(new PricedOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(dbConnectionString)));
            handlers.Add(
                new SeatAssignmentsViewModelGenerator(
                    new ConferenceDao(() => new ConferenceRegistrationDbContext(dbConnectionString)),
                    blobStorage,
                    new JsonTextSerializer()));

            var dispatcher = new EventDispatcher(handlers);
            var events = logReader.Query(new QueryCriteria { EndDate = maxEventTime });

            dispatcher.DispatchMessages(events);
        }
    }
}

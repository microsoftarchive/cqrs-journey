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
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Reflection;
    using Conference;
    using Conference.Common.Utils;
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.MessageLog;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Registration.Events;
    using Registration.Handlers;
    using Registration.ReadModel.Implementation;

    class Program
    {
        // List of hardcoded references to make sure events can be deserialized correctly.
        Assembly[] assemblies = new[] { typeof(ConferenceCreated).Assembly, typeof(AvailableSeatsChanged).Assembly };

        static void Main(string[] args)
        {
            var migrator = new Migrator();
            var confMgmtConnectionString = "DbContext.ConferenceManagement";
            var conferenceRegistrationConnectionString = "DbContext.ConferenceRegistration";
            // var events = migrator.GenerateMissedConferenceManagementIntegrationEvents(confMgmtConnectionString);

            var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var tableName = "TestMigrationLog" + HandleGenerator.Generate(15);
            var logWriter = new AzureMessageLogWriter(storageAccount, tableName);
            var settings = InfrastructureSettings.Read("Settings.xml");
            var eventSourcingSettings = settings.EventSourcing;
            var eventSourcingAccount = CloudStorageAccount.Parse(eventSourcingSettings.ConnectionString);
            try
            {
                migrator.GeneratePastEventLogMessagesForConferenceManagement(logWriter, confMgmtConnectionString, new StandardMetadataProvider(), new JsonTextSerializer());
                migrator.GeneratePastEventLogMessagesForEventSourced(logWriter, eventSourcingAccount.CreateCloudTableClient(), eventSourcingSettings.TableName, new StandardMetadataProvider(), new JsonTextSerializer());
            }
            finally
            {
                var tableClient = storageAccount.CreateCloudTableClient();
                Debugger.Break();
                //tableClient.DeleteTableIfExist(tableName);
            }

            var commandBus = new NullCommandBus();
            var eventBus = new NullEventBus();

            Database.SetInitializer<ConferenceRegistrationDbContext>(null);

            var handlers = new List<IEventHandler>();
            handlers.Add(new ConferenceViewModelGenerator(() => new ConferenceRegistrationDbContext(conferenceRegistrationConnectionString), commandBus));

            var logReader = new AzureEventLogReader(storageAccount, tableName, new JsonTextSerializer());

            using (var context = new ConferenceRegistrationMigrationDbContext(conferenceRegistrationConnectionString))
            {
                context.UpdateTables();
            }

            try
            {
                var replayer = new EventReplayer(handlers);
                var events = logReader.Query(new QueryCriteria { });

                replayer.ReplayEvents(events);
            }
            catch
            {
                using (var context = new ConferenceRegistrationMigrationDbContext(conferenceRegistrationConnectionString))
                {
                    context.UpdateTables();
                }

                throw;
            }
        }
    }
}

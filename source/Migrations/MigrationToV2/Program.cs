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
    using System.Reflection;
    using Conference;
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Registration.Events;

    class Program
    {
        // List of hardcoded references to make sure events can be deserialized correctly.
        Assembly[] assemblies = new[] { typeof(ConferenceCreated).Assembly, typeof(AvailableSeatsChanged).Assembly };

        static void Main(string[] args)
        {
            var migrator = new Migrator();
            var dbConnectionString = "DbContext.ConferenceManagement";

            var settings = InfrastructureSettings.Read("Settings.xml");
            var eventSourcingSettings = settings.EventSourcing;
            var eventSourcingAccount = CloudStorageAccount.Parse(eventSourcingSettings.ConnectionString);
            var originalEventStoreName = "ConferenceEventStore"; // should use the real one. No longer in the updated Settings.xml
            var newEventStoreName = eventSourcingSettings.TableName;
            var messageLogSettings = settings.MessageLog;
            var messageLogAccount = CloudStorageAccount.Parse(messageLogSettings.ConnectionString);

            migrator.GeneratePastEventLogMessagesForConferenceManagement(
                messageLogAccount.CreateCloudTableClient(),
                messageLogSettings.TableName,
                dbConnectionString,
                new StandardMetadataProvider(),
                new JsonTextSerializer());
            migrator.MigrateEventSourcedAndGeneratePastEventLogs(
                messageLogAccount.CreateCloudTableClient(),
                messageLogSettings.TableName,
                eventSourcingAccount.CreateCloudTableClient(),
                originalEventStoreName,
                eventSourcingAccount.CreateCloudTableClient(),
                newEventStoreName,
                new StandardMetadataProvider(),
                new JsonTextSerializer());

            var logReader = new AzureEventLogReader(messageLogAccount, messageLogSettings.TableName, new JsonTextSerializer());
            migrator.RegenerateViewModels(logReader, dbConnectionString);
        }
    }
}

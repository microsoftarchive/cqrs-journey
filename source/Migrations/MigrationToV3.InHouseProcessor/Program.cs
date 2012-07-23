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
    using System.Configuration;
    using System.Reflection;
    using System.Threading;
    using Conference;
    using Infrastructure.Azure;
    using Infrastructure.Azure.BlobStorage;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Registration.Events;

    class Program
    {
        // List of hardcoded references to make sure events can be deserialized correctly.
        private static readonly Assembly[] assemblies = new[] { typeof(ConferenceCreated).Assembly, typeof(AvailableSeatsChanged).Assembly };

        private static readonly TimeSpan WaitTime = TimeSpan.FromMinutes(10);

        static void Main(string[] args)
        {
            var migrator = new Migrator();
            var dbConnectionString = "DbContext.ConferenceManagement";

            var settings = InfrastructureSettings.Read("Settings.xml");
            var messageLogSettings = settings.MessageLog;
            var messageLogAccount = CloudStorageAccount.Parse(messageLogSettings.ConnectionString);
            var blobStorageAccount = CloudStorageAccount.Parse(settings.BlobStorage.ConnectionString);

            DatabaseSetup.Initialize();
            MigrationToV3.Migration.Initialize();


            Console.WriteLine("Creating new read model subscriptions");

            migrator.CreateV3ReadModelSubscriptions(settings.ServiceBus);

            Console.WriteLine("Creating new read model tables");

            migrator.CreateV3ReadModelTables(ConfigurationManager.ConnectionStrings[dbConnectionString].ConnectionString);

            Console.WriteLine("Waiting to let the new subscriptions fill up with events. This will take {0:F0} minutes.", WaitTime.TotalMinutes);

            Thread.Sleep(WaitTime);

            Console.WriteLine("Replaying events to regenerate read models");

            var logReader = new AzureEventLogReader(messageLogAccount, messageLogSettings.TableName, new JsonTextSerializer());
            var blobStorage = new CloudBlobStorage(blobStorageAccount, settings.BlobStorage.RootContainerName);

            var maxEventTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(WaitTime.TotalSeconds / 2));

            migrator.RegenerateV3ViewModels(logReader, blobStorage, dbConnectionString, maxEventTime);

            Console.WriteLine("Set the MaintenanceMode flag to true in the worker role for v2 through the Windows Azure portal, but let the websites keep running. Make sure that the status for the worker role is updated before continuing.");
            Console.WriteLine("Press enter to start processing events.");
            Console.ReadLine();

            using (var processor = new ConferenceProcessor(false))
            {
                processor.Start();

                Console.WriteLine("Started processing events to keep the v2 read models up to date, so there is no downtime until v3 starts functioning.");
                Console.WriteLine("Set the MaintenanceMode flag to false in all the v3 roles. Once you verify that the v3 roles are working correctly in the Staging area, you can do a VIP swap so the public website points to v3.");
                Console.WriteLine("Press enter to finish and stop processing v2 read models (only do this once v3 is in the Production slot). You can also stop the v2 deployment that should be in the Staging slot after the VIP swap");
                Console.ReadLine();

                processor.Stop();
            }
        }
    }
}

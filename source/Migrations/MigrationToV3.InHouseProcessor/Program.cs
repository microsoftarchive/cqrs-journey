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

namespace MigrationToV3.InHouseProcessor
{
    using System;
    using System.Configuration;
    using System.Reflection;
    using System.Threading;
    using Conference;
    using Infrastructure.Azure;
    using Infrastructure.Azure.MessageLog;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Registration.Events;

    class Program
    {
        // List of hardcoded references to make sure events can be deserialized correctly.
        private static readonly Assembly[] assemblies = new[] { typeof(ConferenceCreated).Assembly, typeof(AvailableSeatsChanged).Assembly };

        private static readonly int WaitTimeInMilliseconds = 60 * 1000 * 10;

        static void Main(string[] args)
        {
            var migrator = new Migrator();
            var dbConnectionString = "DbContext.ConferenceManagement";

            var settings = InfrastructureSettings.Read("Settings.xml");
            var messageLogSettings = settings.MessageLog;
            var messageLogAccount = CloudStorageAccount.Parse(messageLogSettings.ConnectionString);

            DatabaseSetup.Initialize();
            MigrationToV3.Migration.Initialize();


            Console.WriteLine("Creating new read model subscriptions");

            migrator.CreateV3ReadModelSubscriptions(settings.ServiceBus);

            Console.WriteLine("Creating new read model tables");

            migrator.CreateV3ReadModelTables(ConfigurationManager.ConnectionStrings[dbConnectionString].ConnectionString);

            Console.WriteLine("Waiting to let the new subscriptions fill up with events");

            Thread.Sleep(WaitTimeInMilliseconds);

            Console.WriteLine("Replaying events to regenerate read models");

            var logReader = new AzureEventLogReader(messageLogAccount, messageLogSettings.TableName, new JsonTextSerializer());
            var maxEventTime = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(WaitTimeInMilliseconds / -2));
            migrator.RegenerateV3ViewModels(logReader, dbConnectionString, maxEventTime);

            Console.WriteLine("Press enter to start processing events. Make sure the v2 processor is disabled.");
            Console.ReadLine();

            using (var processor = new ConferenceProcessor(false))
            {
                processor.Start();

                Console.WriteLine("Host started");
                Console.WriteLine("Press enter to finish");
                Console.ReadLine();

                processor.Stop();
            }
        }
    }
}

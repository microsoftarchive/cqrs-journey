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

using Infrastructure;
using Infrastructure.Azure;
using Infrastructure.Azure.EventSourcing;
using Infrastructure.EventSourcing;
using Infrastructure.Serialization;
using Microsoft.WindowsAzure;

namespace Conference.Specflow.Support
{
    public static class EventSourceHelper
    {
        public static IEventSourcedRepository<T> GetRepository<T>() where T : class, IEventSourced
        {
            var settings = InfrastructureSettings.Read("Settings.xml");
            var eventSourcingAccount = CloudStorageAccount.Parse(settings.EventSourcing.ConnectionString);
            var eventStore = new EventStore(eventSourcingAccount, settings.EventSourcing.TableName);
            var publisher = new EventStoreBusPublisher(ConferenceHelper.GetTopicSender("events"), eventStore);
            var metadata = new StandardMetadataProvider();

            return new AzureEventSourcedRepository<T>(eventStore, publisher, new JsonTextSerializer(), metadata);
        }
    }
}

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

namespace Infrastructure.Azure.IntegrationTests.AzureEventLogFixture
{
    using System;
    using System.Linq;
    using Infrastructure.Azure;
    using Infrastructure.Azure.EventLog;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.EventLog;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Xunit;

    public class given_an_empty_event_log : IDisposable
    {
        private readonly string tableName;
        private CloudStorageAccount account;
        protected AzureEventLog sut;
        protected string sourceId;
        protected string partitionKey;

        public given_an_empty_event_log()
        {
            this.tableName = "AzureEventLogFixture" + new Random((int)DateTime.Now.Ticks).Next();
            var settings = InfrastructureSettings.ReadEventSourcing("Settings.xml");
            this.account = CloudStorageAccount.Parse(settings.ConnectionString);
            this.sut = new AzureEventLog(this.account, this.tableName, new JsonTextSerializer(), new StandardMetadataProvider());

            this.sourceId = Guid.NewGuid().ToString();
            this.partitionKey = Guid.NewGuid().ToString();
        }

        public void Dispose()
        {
            var client = this.account.CreateCloudTableClient();
            client.DeleteTableIfExist(this.tableName);
        }

        [Fact]
        public void when_saving_event_then_can_read_all()
        {
            var e = new FakeEvent { Value = "hello" };

            this.sut.Save(e);

            var result = this.sut.Read().ToList();

            Assert.Equal(1, result.Count);
            Assert.True(result[0] is FakeEvent);
            Assert.Equal("hello", ((FakeEvent)result[0]).Value);
        }

        public class FakeEvent : IEvent
        {
            public string Value { get; set; }
            public Guid SourceId { get; set; }
        }
    }
}

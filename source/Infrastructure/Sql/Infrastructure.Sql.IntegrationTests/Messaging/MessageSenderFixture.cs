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

namespace Infrastructure.Sql.IntegrationTests.Messaging.MessageSenderFixture
{
    using System;
    using System.Data.Entity.Infrastructure;
    using Infrastructure.Sql.Messaging;
    using Infrastructure.Sql.Messaging.Implementation;
    using Xunit;

    public class given_sender : IDisposable
    {
        private readonly IDbConnectionFactory connectionFactory;
        private readonly MessageSender sender;

        public given_sender()
        {
            this.connectionFactory = System.Data.Entity.Database.DefaultConnectionFactory;
            this.sender = new MessageSender(this.connectionFactory, "TestSqlMessaging", "Test.Commands");

            MessagingDbInitializer.CreateDatabaseObjects(this.connectionFactory.CreateConnection("TestSqlMessaging").ConnectionString, "Test", true);
        }

        void IDisposable.Dispose()
        {
            using (var connection = this.connectionFactory.CreateConnection("TestSqlMessaging"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "TRUNCATE TABLE Test.Commands";
                command.ExecuteNonQuery();
            }
        }

        [Fact]
        public void when_sending_string_message_then_saves_message()
        {
            var messageBody = "Message-" + Guid.NewGuid().ToString();
            var message = new Message(messageBody);

            this.sender.Send(message);

            //using (var context = this.contextFactory())
            //{
            //    Assert.True(context.Set<Message>().Any(m => m.Body.Contains(messageBody)));
            //}
        }
    }
}

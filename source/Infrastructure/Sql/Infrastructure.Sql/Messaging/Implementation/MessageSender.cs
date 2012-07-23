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
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure;
    using System.Data.SqlClient;
    using System.Transactions;

    public class MessageSender : IMessageSender
    {
        private readonly IDbConnectionFactory connectionFactory;
        private readonly string name;
        private readonly string insertQuery;

        public MessageSender(IDbConnectionFactory connectionFactory, string name, string tableName)
        {
            this.connectionFactory = connectionFactory;
            this.name = name;
            this.insertQuery = string.Format("INSERT INTO {0} (Body, DeliveryDate, CorrelationId) VALUES (@Body, @DeliveryDate, @CorrelationId)", tableName);
        }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        public void Send(Message message)
        {
            using (var connection = this.connectionFactory.CreateConnection(this.name))
            {
                connection.Open();

                InsertMessage(message, connection);
            }
        }

        /// <summary>
        /// Sends a batch of messages.
        /// </summary>
        public void Send(IEnumerable<Message> messages)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                using (var connection = this.connectionFactory.CreateConnection(this.name))
                {
                    connection.Open();

                    foreach (var message in messages)
                    {
                        this.InsertMessage(message, connection);
                    }
                }

                scope.Complete();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Does not contain user input.")]
        private void InsertMessage(Message message, DbConnection connection)
        {
            using (var command = (SqlCommand)connection.CreateCommand())
            {
                command.CommandText = this.insertQuery;
                command.CommandType = CommandType.Text;

                command.Parameters.Add("@Body", SqlDbType.NVarChar).Value = message.Body;
                command.Parameters.Add("@DeliveryDate", SqlDbType.DateTime).Value = message.DeliveryDate.HasValue ? (object)message.DeliveryDate.Value : DBNull.Value;
                command.Parameters.Add("@CorrelationId", SqlDbType.NVarChar).Value = (object)message.CorrelationId ?? DBNull.Value;

                command.ExecuteNonQuery();
            }
        }
    }
}

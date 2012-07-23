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
    using System.Data;
    using System.Data.Entity.Infrastructure;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    public class MessageReceiver : IMessageReceiver, IDisposable
    {
        private readonly IDbConnectionFactory connectionFactory;
        private readonly string name;
        private readonly string readQuery;
        private readonly string deleteQuery;
        private readonly TimeSpan pollDelay;
        private readonly object lockObject = new object();
        private CancellationTokenSource cancellationSource;

        public MessageReceiver(IDbConnectionFactory connectionFactory, string name, string tableName)
            : this(connectionFactory, name, tableName, TimeSpan.FromMilliseconds(100))
        {
        }

        public MessageReceiver(IDbConnectionFactory connectionFactory, string name, string tableName, TimeSpan pollDelay)
        {
            this.connectionFactory = connectionFactory;
            this.name = name;
            this.pollDelay = pollDelay;

            this.readQuery =
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"SELECT TOP (1) 
                    {0}.[Id] AS [Id], 
                    {0}.[Body] AS [Body], 
                    {0}.[DeliveryDate] AS [DeliveryDate],
                    {0}.[CorrelationId] AS [CorrelationId]
                    FROM {0} WITH (UPDLOCK, READPAST)
                    WHERE ({0}.[DeliveryDate] IS NULL) OR ({0}.[DeliveryDate] <= @CurrentDate)
                    ORDER BY {0}.[Id] ASC",
                    tableName);
            this.deleteQuery =
                string.Format(
                   CultureInfo.InvariantCulture,
                   "DELETE FROM {0} WHERE Id = @Id",
                   tableName);
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived = (sender, args) => { };

        public void Start()
        {
            lock (this.lockObject)
            {
                if (this.cancellationSource == null)
                {
                    this.cancellationSource = new CancellationTokenSource();
                    Task.Factory.StartNew(
                        () => this.ReceiveMessages(this.cancellationSource.Token),
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Current);
                }
            }
        }

        public void Stop()
        {
            lock (this.lockObject)
            {
                using (this.cancellationSource)
                {
                    if (this.cancellationSource != null)
                    {
                        this.cancellationSource.Cancel();
                        this.cancellationSource = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Stop();
        }

        ~MessageReceiver()
        {
            Dispose(false);
        }

        /// <summary>
        /// Receives the messages in an endless loop.
        /// </summary>
        private void ReceiveMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!this.ReceiveMessage())
                {
                    Thread.Sleep(this.pollDelay);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Does not contain user input.")]
        protected bool ReceiveMessage()
        {
            using (var connection = this.connectionFactory.CreateConnection(this.name))
            {
                var currentDate = GetCurrentDate();

                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        long messageId = -1;
                        Message message = null;

                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = CommandType.Text;
                            command.CommandText = this.readQuery;
                            ((SqlCommand)command).Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = currentDate;

                            using (var reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    return false;
                                }

                                var body = (string)reader["Body"];
                                var deliveryDateValue = reader["DeliveryDate"];
                                var deliveryDate = deliveryDateValue == DBNull.Value ? (DateTime?)null : new DateTime?((DateTime)deliveryDateValue);
                                var correlationIdValue = reader["CorrelationId"];
                                var correlationId = (string)(correlationIdValue == DBNull.Value ? null : correlationIdValue);

                                message = new Message(body, deliveryDate, correlationId);
                                messageId = (long)reader["Id"];
                            }
                        }

                        this.MessageReceived(this, new MessageReceivedEventArgs(message));

                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction;
                            command.CommandType = CommandType.Text;
                            command.CommandText = this.deleteQuery;
                            ((SqlCommand)command).Parameters.Add("@Id", SqlDbType.BigInt).Value = messageId;

                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }
                        throw;
                    }
                }
            }


            return true;
        }

        protected virtual DateTime GetCurrentDate()
        {
            return DateTime.UtcNow;
        }
    }
}

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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infrastructure;
using Infrastructure.MessageLog;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Sql.MessageLog;

namespace Conference.Specflow.Support.MessageLog
{
    public class SqlCommandMessageLog : SqlMessageLog, ICommandLogReader
    {
        private readonly string nameOrConnectionString;
        private readonly ITextSerializer serializer;

        public SqlCommandMessageLog(string nameOrConnectionString, ITextSerializer serializer, IMetadataProvider metadataProvider) :
            base(nameOrConnectionString, serializer, metadataProvider)
        {
            this.nameOrConnectionString = nameOrConnectionString;
            this.serializer = serializer;
        }

        public new IEnumerable<ICommand> Query(QueryCriteria criteria)
        {
            return new SqlCommandQuery(this.nameOrConnectionString, this.serializer, criteria);
        }

        private class SqlCommandQuery : IEnumerable<ICommand>
        {
            private readonly string nameOrConnectionString;
            private readonly ITextSerializer serializer;
            private readonly QueryCriteria criteria;

            public SqlCommandQuery(string nameOrConnectionString, ITextSerializer serializer, QueryCriteria criteria)
            {
                this.nameOrConnectionString = nameOrConnectionString;
                this.serializer = serializer;
                this.criteria = criteria;
            }

            public IEnumerator<ICommand> GetEnumerator()
            {
                return new DisposingEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class DisposingEnumerator : IEnumerator<ICommand>
            {
                private SqlCommandQuery sqlQuery;
                private MessageLogDbContext context;
                private IEnumerator<ICommand> commands;

                public DisposingEnumerator(SqlCommandQuery sqlQuery)
                {
                    this.sqlQuery = sqlQuery;
                }

                ~DisposingEnumerator()
                {
                    if (context != null) context.Dispose();
                }

                public void Dispose()
                {
                    if (context != null)
                    {
                        context.Dispose();
                        context = null;
                        GC.SuppressFinalize(this);
                    }
                    if (commands != null)
                    {
                        try { commands.Dispose(); }catch(ObjectDisposedException) { }
                    }
                }

                public ICommand Current { get { return commands.Current; } }
                object IEnumerator.Current { get { return this.Current; } }

                public bool MoveNext()
                {
                    if (context == null)
                    {
                        context = new MessageLogDbContext(sqlQuery.nameOrConnectionString);
                        var queryable = context.Set<MessageLogEntity>().AsQueryable()
                            .Where(x => x.Kind == StandardMetadata.CommandKind);

                        var where = sqlQuery.criteria.ToExpression();
                        if (where != null)
                            queryable = queryable.Where(where);

                        commands = queryable
                            .AsEnumerable()
                            .Select(x => this.sqlQuery.serializer.Deserialize<ICommand>(x.Payload))
                            .GetEnumerator();
                    }

                    return commands.MoveNext();
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}

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

namespace Infrastructure.Sql.EventLog
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.EventLog;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;

    public class SqlEventLog : IEventLog
    {
        private string nameOrConnectionString;
        private IMetadataProvider metadataProvider;
        private ITextSerializer serializer;

        public SqlEventLog(string nameOrConnectionString, ITextSerializer serializer, IMetadataProvider metadataProvider)
        {
            this.nameOrConnectionString = nameOrConnectionString;
            this.serializer = serializer;
            this.metadataProvider = metadataProvider;
        }

        public void Save(IEvent @event)
        {
            using (var context = new EventLogDbContext(this.nameOrConnectionString))
            {
                var metadata = this.metadataProvider.GetMetadata(@event);

                context.Set<EventLogEntity>().Add(new EventLogEntity
                {
                    Id = Guid.NewGuid(),
                    SourceId = @event.SourceId.ToString(),
                    AssemblyName = metadata[StandardMetadata.AssemblyName],
                    FullName = metadata[StandardMetadata.FullName],
                    Namespace = metadata[StandardMetadata.Namespace],
                    TypeName = metadata[StandardMetadata.TypeName],
                    Payload = serializer.Serialize(@event),
                });
                context.SaveChanges();
            }
        }

        public IEnumerable<IEvent> Query(QueryCriteria criteria)
        {
            return new SqlQuery(this.nameOrConnectionString, this.serializer, criteria);
        }

        private class SqlQuery : IEnumerable<IEvent>
        {
            private string nameOrConnectionString;
            private ITextSerializer serializer;
            private QueryCriteria criteria;

            public SqlQuery(string nameOrConnectionString, ITextSerializer serializer, QueryCriteria criteria)
            {
                this.nameOrConnectionString = nameOrConnectionString;
                this.serializer = serializer;
                this.criteria = criteria;
            }

            public IEnumerator<IEvent> GetEnumerator()
            {
                return new DisposingEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class DisposingEnumerator : IEnumerator<IEvent>
            {
                private SqlQuery sqlQuery;
                private EventLogDbContext context;
                private IEnumerator<IEvent> events;

                public DisposingEnumerator(SqlQuery sqlQuery)
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
                    if (events != null)
                    {
                        events.Dispose();
                    }
                }

                public IEvent Current { get { return events.Current; } }
                object IEnumerator.Current { get { return this.Current; } }

                public bool MoveNext()
                {
                    if (context == null)
                    {
                        context = new EventLogDbContext(sqlQuery.nameOrConnectionString);
                        var queryable = (IQueryable<EventLogEntity>)context.Events;
                        var where = sqlQuery.criteria.ToExpression();
                        if (where != null)
                            queryable = queryable.Where(where);

                        events = queryable
                            .AsEnumerable()
                            .Select(x => this.sqlQuery.serializer.Deserialize<IEvent>(x.Payload))
                            .GetEnumerator();
                    }

                    return events.MoveNext();
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}

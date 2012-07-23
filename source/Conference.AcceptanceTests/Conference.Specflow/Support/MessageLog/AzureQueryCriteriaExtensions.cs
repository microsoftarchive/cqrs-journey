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

namespace Infrastructure.Azure.MessageLog
{
    using System;
    using System.Linq.Expressions;
    using Infrastructure.MessageLog;

    internal static class QueryCriteriaExtensions
    {
        public static Expression<Func<MessageLogEntity, bool>> ToExpression(this QueryCriteria criteria)
        {
            // The full Where clause being built.
            Expression<Func<MessageLogEntity, bool>> expression = null;

            foreach (var asm in criteria.AssemblyNames)
            {
                var value = asm;
                if (expression == null)
                    expression = e => e.AssemblyName == value;
                else
                    expression = expression.Or(e => e.AssemblyName == value);
            }

            // The current criteria filter being processed (i.e. FullName).
            Expression<Func<MessageLogEntity, bool>> filter = null;
            foreach (var item in criteria.FullNames)
            {
                var value = item;
                if (filter == null)
                    filter = e => e.FullName == value;
                else
                    filter = filter.Or(e => e.FullName == value);
            }

            if (filter != null)
            {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }

            foreach (var item in criteria.Namespaces)
            {
                var value = item;
                if (filter == null)
                    filter = e => e.Namespace == value;
                else
                    filter = filter.Or(e => e.Namespace == value);
            }

            if (filter != null)
            {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }

            foreach (var item in criteria.SourceIds)
            {
                var value = item;
                if (filter == null)
                    filter = e => e.SourceId == value;
                else
                    filter = filter.Or(e => e.SourceId == value);
            }

            if (filter != null)
            {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }

            foreach (var item in criteria.SourceTypes)
            {
                var value = item;
                if (filter == null)
                    filter = e => e.SourceType == value;
                else
                    filter = filter.Or(e => e.SourceType == value);
            }

            if (filter != null)
            {
                expression = (expression == null) ? filter : expression.And(filter);
                filter = null;
            }

            foreach (var item in criteria.TypeNames)
            {
                var value = item;
                if (filter == null)
                    filter = e => e.TypeName == value;
                else
                    filter = filter.Or(e => e.TypeName == value);
            }

            if (filter != null)
            {
                expression = (expression == null) ? filter : expression.And(filter);
            }

            return expression;
        }
    }
}

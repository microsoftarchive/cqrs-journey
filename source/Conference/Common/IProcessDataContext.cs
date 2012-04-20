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

namespace Common
{
    using System;
    using System.Linq.Expressions;

    // TODO: Does this even belong to a reusable infrastructure?
    // This for reading and writing processes (aka Sagas in the CQRS community)
    public interface IProcessDataContext<T> : IDisposable
        where T : class, IProcess
    {
        T Find(Guid id);

        void Save(T process);

        // TODO: queryability to reload processes from correlation ids, etc. 
        // Is this appropriate? How do others reload processes? (MassTransit 
        // uses this kind of queryable thinghy, apparently).
        //IEnumerable<T> Query(Expression<Func<T, bool>> predicate)
        T Find(Expression<Func<T, bool>> predicate);
    }
}

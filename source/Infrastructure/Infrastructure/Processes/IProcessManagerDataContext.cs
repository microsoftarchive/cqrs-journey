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

namespace Infrastructure.Processes
{
    using System;
    using System.Linq.Expressions;

    // TODO: Does this even belong to a reusable infrastructure?
    // This for reading and writing process managers (also known as Sagas in the CQRS community)
    public interface IProcessManagerDataContext<T> : IDisposable
        where T : class, IProcessManager
    {
        T Find(Guid id);

        void Save(T processManager);

        // TODO: queryability to reload processes from correlation ids, etc. 
        // Is this appropriate? How do others reload processes? (MassTransit 
        // uses this kind of queryable approach, apparently).
        T Find(Expression<Func<T, bool>> predicate, bool includeCompleted = false);
    }
}

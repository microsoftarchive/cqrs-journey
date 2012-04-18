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

    public interface IProcessRepository
	{
        T Find<T>(Guid id) where T : class, IAggregateRoot;

        void Save<T>(T process) where T : class, IAggregateRoot;

        // TODO: queryability to reload processes from correlation ids, etc. 
		// Is this appropriate? How do others reload processes? (MassTransit 
		// uses this kind of queryable thinghy, apparently).
        //IEnumerable<T> Query<T>(Expression<Func<T, bool>> predicate) where T : class, IAggregateRoot;
        T Find<T>(Expression<Func<T, bool>> predicate) where T : class, IAggregateRoot;
    }
}

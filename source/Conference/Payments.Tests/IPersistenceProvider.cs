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

namespace Payments.Tests
{
    using System;
    using Infrastructure.Database;

    /// <summary>
    /// Provides a way to abstract the work a persistence layer would perform 
    /// so that test code can be reused against in-memory and DB tests.
    /// </summary>
    public interface IPersistenceProvider : IDisposable
    {
        /// <summary>
        /// Persists and reloads the aggregate, so that associated 
        /// persistence behavior is exercised as needed.
        /// </summary>
        T PersistReload<T>(T sut) where T : class, IAggregateRoot;
    }

    /// <summary>
    /// Provides a fast no-op provider for unit tests to use.
    /// </summary>
    public class NoPersistenceProvider : IPersistenceProvider
    {
        public T PersistReload<T>(T sut)
            where T : class, IAggregateRoot
        {
            return sut;
        }

        public void Dispose()
        {
        }
    }
}

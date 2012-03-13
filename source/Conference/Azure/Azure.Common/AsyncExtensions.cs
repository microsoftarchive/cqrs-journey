// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Azure
{
    using System;

    /// <summary>
    /// Extension methods to make Begin/End pattern more readable.
    /// </summary>
    public static class AsyncExtensions
    {
        /// <summary>
        /// Invokes the given <paramref name="begin"/> method passing the 
        /// specified <paramref name="arg"/> and calling <paramref name="end"/> 
        /// asynchronously when it ends.
        /// </summary>
        public static void Async<T, TArg>(this T target, TArg arg, Func<TArg, AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            begin(arg, new AsyncCallback(end), target);
        }

        /// <summary>
        /// Invokes the given <paramref name="begin"/> method and calls <paramref name="end"/> asynchronously when it ends.
        /// </summary>
        public static void Async<T>(this T target, Func<AsyncCallback, object, IAsyncResult> begin, Action<IAsyncResult> end)
        {
            begin(new AsyncCallback(end), target);
        }
    }
}

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

namespace Infrastructure.Azure.Utils
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal class TaskEx
    {
        /// <summary>
        /// Starts a Task that will complete after the specified due time.
        /// </summary>
        /// <param name="dueTime">The delay in milliseconds before the returned task completes.</param>
        /// <returns>
        /// The timed Task.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Timer is disposed in the timer callback")]
        public static Task Delay(int dueTime)
        {
            if (dueTime <= 0) throw new ArgumentOutOfRangeException("dueTime");

            var tcs = new TaskCompletionSource<bool>();
            var timer = new Timer(self =>
            {
                ((Timer)self).Dispose();
                tcs.TrySetResult(true);
            });
            timer.Change(dueTime, Timeout.Infinite);
            return tcs.Task;
        }
    }
}

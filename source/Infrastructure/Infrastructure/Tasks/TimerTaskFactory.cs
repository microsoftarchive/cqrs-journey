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

namespace Infrastructure.Tasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class TimerTaskFactory
    {
        public static Task<T> StartNew<T>(Func<T> getResult, Func<T, bool> isResultValid, long milliseconds, DateTime expirationTime)
        {
            Timer timer = null;
            TaskCompletionSource<T> taskCompletionSource = null;

            timer =
                new Timer(_ =>
                {
                    try
                    {
                        if (DateTime.Now > expirationTime)
                        {
                            timer.Dispose();
                            taskCompletionSource.SetResult(default(T));
                            return;
                        }

                        var result = getResult();

                        if (isResultValid(result))
                        {
                            timer.Dispose();
                            taskCompletionSource.SetResult(result);
                        }
                        else
                        {
                            // try again
                            timer.Change(milliseconds, Timeout.Infinite);
                        }
                    }
                    catch (Exception e)
                    {
                        timer.Dispose();
                        taskCompletionSource.SetException(e);
                    }
                });

            taskCompletionSource = new TaskCompletionSource<T>(timer);

            timer.Change(milliseconds, Timeout.Infinite);

            return taskCompletionSource.Task;
        }
    }
}

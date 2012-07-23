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

using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Conference.Common.Entity;
using TechTalk.SpecFlow;
using WorkerRoleCommandProcessor;

namespace Conference.Specflow.Support
{
    [Binding]
    public class SpecFlowEvents
    {
        private static Task processorTask;
        private static CancellationTokenSource tokenSource;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);
            
            tokenSource = new CancellationTokenSource();
            processorTask = Task.Factory.StartNew(() =>
            {
                using (var processor = new ConferenceProcessor())
                {
                    try
                    {
                        processor.Start();
                        WaitHandle.WaitAny(new[] {tokenSource.Token.WaitHandle});
                    }
                    finally
                    {
                        processor.Stop();
                    }
                }
            }, tokenSource.Token);
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }
}

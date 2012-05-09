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

namespace Registration.IntegrationTests
{
    using System;
    using Registration.Database;
    using Xunit;

    public class RegistrationProcessDbContextFixture
    {
        [Fact]
        public void when_saving_process_then_can_retrieve_it()
        {
            var dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new RegistrationProcessDbContext(dbName))
            {
                context.Database.Create();
            }

            try
            {
                Guid id = Guid.Empty;
                using (var context = new RegistrationProcessDbContext(dbName))
                {
                    var process = new RegistrationProcess();
                    context.RegistrationProcesses.Add(process);
                    context.SaveChanges();
                    id = process.Id;
                }
                using (var context = new RegistrationProcessDbContext(dbName))
                {
                    var process = context.RegistrationProcesses.Find(id);
                    Assert.NotNull(process);
                }
            }
            finally
            {
                using (var context = new RegistrationProcessDbContext(dbName))
                {
                    context.Database.Delete();
                }
            }
        }

    }
}

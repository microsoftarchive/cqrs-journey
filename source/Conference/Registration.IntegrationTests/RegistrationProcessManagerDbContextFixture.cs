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

namespace Registration.IntegrationTests
{
    using System;
    using System.Data.Entity.Infrastructure;
    using Registration.Database;
    using Xunit;

    public class RegistrationProcessManagerDbContextFixture
    {
        [Fact]
        public void when_saving_process_then_can_retrieve_it()
        {
            var dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new RegistrationProcessManagerDbContext(dbName))
            {
                context.Database.Create();
            }

            try
            {
                Guid id = Guid.Empty;
                using (var context = new RegistrationProcessManagerDbContext(dbName))
                {
                    var pm = new RegistrationProcessManager();
                    context.RegistrationProcesses.Add(pm);
                    context.SaveChanges();
                    id = pm.Id;
                }
                using (var context = new RegistrationProcessManagerDbContext(dbName))
                {
                    var pm = context.RegistrationProcesses.Find(id);
                    Assert.NotNull(pm);
                }
            }
            finally
            {
                using (var context = new RegistrationProcessManagerDbContext(dbName))
                {
                    context.Database.Delete();
                }
            }
        }

        [Fact]
        public void when_saving_process_performs_optimistic_locking()
        {
            var dbName = this.GetType().Name + "-" + Guid.NewGuid();
            using (var context = new RegistrationProcessManagerDbContext(dbName))
            {
                context.Database.Create();
            }

            try
            {
                Guid id = Guid.Empty;
                using (var context = new RegistrationProcessManagerDbContext(dbName))
                {
                    var pm = new RegistrationProcessManager();
                    context.RegistrationProcesses.Add(pm);
                    context.SaveChanges();
                    id = pm.Id;
                }

                using (var context = new RegistrationProcessManagerDbContext(dbName))
                {
                    var pm = context.RegistrationProcesses.Find(id);

                    pm.State = RegistrationProcessManager.ProcessState.PaymentConfirmationReceived;

                    using (var innerContext = new RegistrationProcessManagerDbContext(dbName))
                    {
                        var innerProcess = innerContext.RegistrationProcesses.Find(id);

                        innerProcess.State = RegistrationProcessManager.ProcessState.ReservationConfirmationReceived;

                        innerContext.SaveChanges();
                    }

                    Assert.Throws<DbUpdateConcurrencyException>(() => context.SaveChanges());
                }
            }
            finally
            {
                using (var context = new RegistrationProcessManagerDbContext(dbName))
                {
                    context.Database.Delete();
                }
            }
        }
    }
}

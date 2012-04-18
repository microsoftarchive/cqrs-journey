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

namespace Registration
{
    using System;
    using Common;
    using Registration.Commands;
    using Registration.Events;

    public class RegistrationProcessRouter :
        IEventHandler<OrderPlaced>,
        IEventHandler<PaymentReceived>,
        IEventHandler<SeatsReserved>,
        ICommandHandler<ExpireRegistrationProcess>
    {
        private readonly object lockObject = new object();
        private readonly Func<IProcessRepository> repositoryFactory;

        public RegistrationProcessRouter(Func<IProcessRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            var process = new RegistrationProcess();
            process.Handle(@event);

            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    repo.Save(process);
                }
            }
        }

        public void Handle(SeatsReserved @event)
        {
            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    var process = repo.Find<RegistrationProcess>(x => x.ReservationId == @event.ReservationId && x.StateValue != (int)RegistrationProcess.ProcessState.Completed);
                    if (process != null)
                    {
                        process.Handle(@event);

                        repo.Save(process);
                    }
                }
            }
        }

        public void Handle(ExpireRegistrationProcess command)
        {
            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    var process = repo.Find<RegistrationProcess>(x => x.Id == command.ProcessId && x.StateValue != (int)RegistrationProcess.ProcessState.Completed);
                    if (process != null)
                    {
                        process.Handle(command);

                        repo.Save(process);
                    }
                }
            }
        }

        public void Handle(PaymentReceived @event)
        {
            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    var process = repo.Find<RegistrationProcess>(x => x.OrderId == @event.OrderId && x.StateValue != (int)RegistrationProcess.ProcessState.Completed);
                    if (process != null)
                    {
                        process.Handle(@event);

                        repo.Save(process);
                    }
                }
            }
        }
    }
}

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
    using System.Linq;
    using Common;
    using Registration.Commands;
    using Registration.Events;

    // TODO: this is to showcase how a handler could be written. No unit tests created yet. Do ASAP.
    public class RegistrationProcessSagaRouter :
        IEventHandler<OrderPlaced>,
        IEventHandler<PaymentReceived>,
        IEventHandler<ReservationAccepted>,
        IEventHandler<ReservationRejected>,
        ICommandHandler<ExpireOrder>
    {
        private object lockObject = new object();
        private Func<ISagaRepository> repositoryFactory;

        public RegistrationProcessSagaRouter(Func<ISagaRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            var saga = new RegistrationProcessSaga();
            saga.Handle(@event);

            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    repo.Save(saga);
                }
            }
        }

        public void Handle(ReservationAccepted @event)
        {
            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    var saga = repo.Query<RegistrationProcessSaga>().FirstOrDefault(x => x.ReservationId == @event.ReservationId);
                    if (saga != null)
                    {
                        saga.Handle(@event);

                        repo.Save(saga);
                    }
                }
            }
        }

        public void Handle(ReservationRejected @event)
        {
            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    var saga = repo.Query<RegistrationProcessSaga>().FirstOrDefault(x => x.ReservationId == @event.ReservationId);
                    if (saga != null)
                    {
                        saga.Handle(@event);

                        repo.Save(saga);
                    }
                }
            }
        }

        public void Handle(ExpireOrder command)
        {
            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                lock (lockObject)
                {
                    var saga = repo.Query<RegistrationProcessSaga>().FirstOrDefault(x => x.OrderId == command.OrderId);
                    if (saga != null)
                    {
                        saga.Handle(command);

                        repo.Save(saga);
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
                    var saga = repo.Query<RegistrationProcessSaga>().FirstOrDefault(x => x.OrderId == @event.OrderId);
                    if (saga != null)
                    {
                        saga.Handle(@event);

                        repo.Save(saga);
                    }
                }
            }
        }
    }
}

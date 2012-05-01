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
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Processes;
    using Payments.Contracts.Events;
    using Registration.Commands;
    using Registration.Events;

    public class RegistrationProcessRouter :
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderUpdated>,
        IEventHandler<OrderReservationCompleted>,
        IEventHandler<PaymentCompleted>,
        IEventHandler<SeatsReserved>,
        ICommandHandler<ExpireRegistrationProcess>
    {
        private readonly object lockObject = new object();
        private readonly Func<IProcessDataContext<RegistrationProcess>> contextFactory;

        public RegistrationProcessRouter(Func<IProcessDataContext<RegistrationProcess>> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            var process = new RegistrationProcess();
            process.Handle(@event);

            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    context.Save(process);
                }
            }
        }

        public void Handle(OrderUpdated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    var process = context.Find(x => x.OrderId == @event.SourceId && x.Completed == false);
                    if (process != null)
                    {
                        process.Handle(@event);

                        context.Save(process);
                    }
                }
            }
        }

        public void Handle(SeatsReserved @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    var process = context.Find(x => x.ReservationId == @event.ReservationId && x.Completed == false);
                    if (process != null)
                    {
                        process.Handle(@event);

                        context.Save(process);
                    }
                }
            }
        }

        public void Handle(OrderReservationCompleted @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    var process = context.Find(x => x.OrderId == @event.SourceId && x.Completed == false);
                    if (process != null)
                    {
                        process.Handle(@event);

                        context.Save(process);
                    }
                }
            }
        }

        public void Handle(ExpireRegistrationProcess command)
        {
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    var process = context.Find(x => x.Id == command.ProcessId && x.Completed == false);
                    if (process != null)
                    {
                        process.Handle(command);

                        context.Save(process);
                    }
                }
            }
        }

        public void Handle(PaymentCompleted @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    var process = context.Find(x => x.OrderId == @event.PaymentSourceId && x.Completed == false);
                    if (process != null)
                    {
                        process.Handle(@event);

                        context.Save(process);
                    }
                }
            }
        }
    }
}

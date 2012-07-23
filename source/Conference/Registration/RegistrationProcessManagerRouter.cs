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

namespace Registration
{
    using System;
    using System.Diagnostics;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Processes;
    using Payments.Contracts.Events;
    using Registration.Commands;
    using Registration.Events;

    /// <summary>
    /// Routes messages (commands and events) to the <see cref="RegistrationProcessManager"/>.
    /// </summary>
    public class RegistrationProcessManagerRouter :
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderUpdated>,
        IEnvelopedEventHandler<SeatsReserved>,
        IEventHandler<PaymentCompleted>,
        IEventHandler<OrderConfirmed>,
        ICommandHandler<ExpireRegistrationProcess>
    {
        private readonly Func<IProcessManagerDataContext<RegistrationProcessManager>> contextFactory;

        public RegistrationProcessManagerRouter(Func<IProcessManagerDataContext<RegistrationProcessManager>> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var pm = context.Find(x => x.OrderId == @event.SourceId);
                if (pm == null)
                {
                    pm = new RegistrationProcessManager();
                }

                pm.Handle(@event);
                context.Save(pm);
            }
        }

        public void Handle(OrderUpdated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var pm = context.Find(x => x.OrderId == @event.SourceId);
                if (pm != null)
                {
                    pm.Handle(@event);

                    context.Save(pm);
                }
                else
                {
                    Trace.TraceError("Failed to locate the registration process manager handling the order with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(Envelope<SeatsReserved> envelope)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var pm = context.Find(x => x.ReservationId == envelope.Body.ReservationId);
                if (pm != null)
                {
                    pm.Handle(envelope);

                    context.Save(pm);
                }
                else
                {
                    // TODO: should Cancel seat reservation!
                    Trace.TraceError("Failed to locate the registration process manager handling the seat reservation with id {0}. TODO: should Cancel seat reservation!", envelope.Body.ReservationId);
                }
            }
        }

        public void Handle(OrderConfirmed @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var pm = context.Find(x => x.OrderId == @event.SourceId);
                if (pm != null)
                {
                    pm.Handle(@event);

                    context.Save(pm);
                }
                else
                {
                    Trace.TraceInformation("Failed to locate the registration process manager to complete with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(PaymentCompleted @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                // TODO: should not skip the completed processes and try to re-acquire the reservation,
                // and if not possible due to not enough seats, move them to a "manual intervention" state.
                // This was not implemented but would be very important.
                var pm = context.Find(x => x.OrderId == @event.PaymentSourceId);
                if (pm != null)
                {
                    pm.Handle(@event);

                    context.Save(pm);
                }
                else
                {
                    Trace.TraceError("Failed to locate the registration process manager handling the paid order with id {0}.", @event.PaymentSourceId);
                }
            }
        }

        public void Handle(ExpireRegistrationProcess command)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var pm = context.Find(x => x.Id == command.ProcessId);
                if (pm != null)
                {
                    pm.Handle(command);

                    context.Save(pm);
                }
            }
        }
    }
}

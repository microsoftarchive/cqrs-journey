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
    using System.Diagnostics;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Processes;
    using Payments.Contracts.Events;
    using Registration.Commands;
    using Registration.Events;

    public class RegistrationProcessRouter :
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderUpdated>,
        IEnvelopedEventHandler<SeatsReserved>,
        IEventHandler<PaymentCompleted>,
        IEventHandler<OrderConfirmed>,
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
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    var process = context.Find(x => x.OrderId == @event.SourceId && x.Completed == false);
                    if (process == null)
                    {
                        // If the process already exists, it means that the OrderPlaced event is being reprocessed because the message 
                        // could not be completed. No need to handle it again.
                        process = new RegistrationProcess();
                        process.Handle(@event);

                        context.Save(process);
                    }
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
                    else
                    {
                        Trace.TraceError("Failed to locate the registration process handling the order with id {0}.", @event.SourceId);
                    }
                }
            }
        }

        public void Handle(ReceiveEnvelope<SeatsReserved> envelope)
        {
            using (var context = this.contextFactory.Invoke())
            {
                lock (lockObject)
                {
                    Guid correlationId;
                    if (!Guid.TryParse(envelope.CorrelationId, out correlationId))
                    {
                        Trace.TraceWarning("Seat reservation response for reservation id {0} does not include message correlation id.", envelope.Body.ReservationId);
                        return;
                    }

                    var process = context.Find(x => x.ReservationId == envelope.Body.ReservationId && x.Completed == false);
                    if (process != null)
                    {
                        if (process.SeatReservationCommandId == correlationId)
                        {
                            process.Handle(envelope.Body);

                            context.Save(process);
                        }
                        else
                        {
                            Trace.TraceWarning(
                                "Ignoring SeatsReserved event. Correlation id {0} does not match the expected correlation id {1} for reservation with id {2}.",
                                correlationId,
                                process.SeatReservationCommandId,
                                process.ReservationId);
                        }
                    }
                    else
                    {
                        Trace.TraceError("Failed to locate the registration process handling the seat reservation with id {0}.", envelope.Body.ReservationId);
                    }
                }
            }
        }

        public void Handle(OrderConfirmed @event)
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
                    else
                    {
                        Trace.TraceInformation("Failed to locate the registration process to complete with id {0}.", @event.SourceId);
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
                    // TODO should not skip the completed processes and move them to a "manual intervention" state
                    var process = context.Find(x => x.OrderId == @event.PaymentSourceId && x.Completed == false);
                    if (process != null)
                    {
                        process.Handle(@event);

                        context.Save(process);
                    }
                    else
                    {
                        Trace.TraceError("Failed to locate the registration process handling the paid order with id {0}.", @event.PaymentSourceId);
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
    }
}

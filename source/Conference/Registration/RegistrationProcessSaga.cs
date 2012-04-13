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
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Common;
    using Registration.Commands;
    using Registration.Events;

    public class RegistrationProcessSaga : IAggregateRoot, ICommandPublisher
    {
        public enum SagaState
        {
            NotStarted = 0,
            AwaitingReservationConfirmation = 1,
            AwaitingPayment = 2,
            Completed = 0xFF,
        }

        private List<Envelope<ICommand>> commands = new List<Envelope<ICommand>>();

        public RegistrationProcessSaga()
        {
            this.Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }
        public Guid ConferenceId { get; set; }
        public Guid OrderId { get; internal set; }
        public Guid ReservationId { get; internal set; }

        // feels akward and possibly disrupting to store these properties here. Would it be better if instead of using 
        // current state values, we use event sourcing?
        public DateTime? ReservationAutoExpiration { get; internal set; }
        public Guid ExpirationCommandId { get; set; }

        public int StateValue { get; private set; }
        [NotMapped]
        public SagaState State
        {
            get { return (SagaState)this.StateValue; }
            internal set { this.StateValue = (int)value; }
        }

        public IEnumerable<Envelope<ICommand>> Commands
        {
            get { return this.commands; }
        }

        public void Handle(OrderPlaced message)
        {
            if (this.State == SagaState.NotStarted)
            {
                this.ConferenceId = message.ConferenceId;
                this.OrderId = message.OrderId;
                this.ReservationId = Guid.NewGuid();
                this.ReservationAutoExpiration = message.ReservationAutoExpiration;
                this.State = SagaState.AwaitingReservationConfirmation;

                this.AddCommand(
                    new MakeSeatReservation
                    {
                        ConferenceId = message.ConferenceId,
                        ReservationId = this.ReservationId,
                        Seats = message.Seats.ToList()
                    });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Handle(SeatsReserved message)
        {
            if (this.State == SagaState.AwaitingReservationConfirmation)
            {
                var bufferTime = TimeSpan.FromMinutes(5);
                var expirationTime = this.ReservationAutoExpiration.Value;
                
                this.State = SagaState.AwaitingPayment;

                var expirationCommand = new ExpireRegistrationProcess { ProcessId = this.Id };
                this.ExpirationCommandId = expirationCommand.Id;

                this.AddCommand(new Envelope<ICommand>(expirationCommand)
                {
                    Delay = expirationTime.Subtract(DateTime.UtcNow).Add(bufferTime),
                });
                this.AddCommand(new MarkSeatsAsReserved
                {
                    OrderId = this.OrderId,
                    Seats = message.ReservationDetails.ToList(),
                    Expiration = expirationTime,
                });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Handle(ExpireRegistrationProcess message)
        {
            if (this.State == SagaState.AwaitingPayment)
            {
                if (this.ExpirationCommandId == message.Id)
                {
                    this.State = SagaState.Completed;

                    this.AddCommand(new CancelSeatReservation
                    {
                        ConferenceId = this.ConferenceId,
                        ReservationId = this.ReservationId,
                    });
                    this.AddCommand(new RejectOrder { OrderId = this.OrderId });
                }
            }

            // else ignore the message as it is no longer relevant (but not invalid)
        }

        public void Handle(PaymentReceived message)
        {
            if (this.State == SagaState.AwaitingPayment)
            {
                this.ExpirationCommandId = Guid.Empty;
                this.State = SagaState.Completed;

                this.AddCommand(new CommitSeatReservation
                {
                    ReservationId = this.ReservationId,
                    ConferenceId = message.ConferenceId
                });

                this.AddCommand(new ConfirmOrderPayment
                {
                    OrderId = message.OrderId
                });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void AddCommand<T>(T command)
            where T : ICommand
        {
            this.commands.Add(Envelope.Create<ICommand>(command));
        }

        private void AddCommand(Envelope<ICommand> envelope)
        {
            this.commands.Add(envelope);
        }
    }
}
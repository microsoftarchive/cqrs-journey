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
        public Guid OrderId { get; internal set; }
        public Guid ReservationId { get; internal set; }

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
                this.OrderId = message.OrderId;
                this.ReservationId = Guid.NewGuid();
                this.State = SagaState.AwaitingReservationConfirmation;

                this.AddCommand(
                    new MakeSeatReservation
                    {
                        ConferenceId = message.ConferenceId,
                        ReservationId = this.ReservationId,
                        NumberOfSeats = message.Items.Sum(x => x.Quantity)
                    });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Handle(ReservationAccepted message)
        {
            if (this.State == SagaState.AwaitingReservationConfirmation)
            {
                this.State = SagaState.AwaitingPayment;

                this.AddCommand(new MarkOrderAsBooked { OrderId = this.OrderId });
                this.commands.Add(
                    new Envelope<ICommand>(new ExpireOrder { OrderId = this.OrderId, ConferenceId = message.ConferenceId })
                    {
                        Delay = TimeSpan.FromMinutes(15),
                    });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Handle(ReservationRejected message)
        {
            if (this.State == SagaState.AwaitingReservationConfirmation)
            {
                this.State = SagaState.Completed;
                this.AddCommand(new RejectOrder { OrderId = this.OrderId });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Handle(ExpireOrder message)
        {
            if (this.State == SagaState.AwaitingPayment)
            {
                this.State = SagaState.Completed;

                this.AddCommand(new CancelSeatReservation
                {
                    ReservationId = this.ReservationId,
                    ConferenceId = message.ConferenceId
                });
                this.AddCommand(new RejectOrder { OrderId = message.OrderId });
            }

            // else ignore the message as it is no longer relevant (but not invalid)
        }

        public void Handle(PaymentReceived message)
        {
            if (this.State == SagaState.AwaitingPayment)
            {
                this.State = SagaState.Completed;

                this.AddCommand(new CommitSeatReservation
                {
                    ReservationId = this.ReservationId,
                    ConferenceId = message.ConferenceId
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
    }
}
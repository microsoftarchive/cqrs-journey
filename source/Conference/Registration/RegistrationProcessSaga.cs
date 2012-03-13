// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
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
    using System.Linq;
    using Common;
    using Registration.Commands;
    using Registration.Events;

    public class RegistrationProcessSaga : IAggregateRoot, ICommandPublisher
    {
        public static class SagaState
        {
            public const int NotStarted = 0;
            public const int AwaitingReservationConfirmation = 1;
            public const int AwaitingPayment = 2;
            public const int Completed = 0xFF;
        }

        private List<Envelope<ICommand>> commands = new List<Envelope<ICommand>>();

        public Guid Id { get; set; }

        public int State { get; set; }

        public IEnumerable<Envelope<ICommand>> Commands
        {
            get { return this.commands; }
        }

        public void Handle(OrderPlaced message)
        {
            if (this.State == SagaState.NotStarted)
            {
                this.Id = message.OrderId;
                this.State = SagaState.AwaitingReservationConfirmation;
                this.commands.Add(
                    new MakeSeatReservation
                    {
                        Id = this.Id,
                        ConferenceId = message.ConferenceId,
                        ReservationId = message.OrderId,
                        NumberOfSeats = message.Tickets.Sum(x => x.Quantity)
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
                this.commands.Add(new MarkOrderAsBooked { OrderId = message.ReservationId });
                this.commands.Add(
                    new Envelope<ICommand>(new ExpireReservation { Id = message.ReservationId, ConferenceId = message.ConferenceId })
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
                this.commands.Add(new RejectOrder { OrderId = message.ReservationId });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Handle(ExpireReservation message)
        {
            if (this.State == SagaState.AwaitingPayment)
            {
                this.State = SagaState.Completed;
                this.commands.Add(new CancelSeatReservation { ReservationId = message.Id, ConferenceId = message.ConferenceId });
                this.commands.Add(new RejectOrder { OrderId = message.Id });
            }

            // else ignore the message as it is no longer relevant (but not invalid)
        }

        public void Handle(PaymentReceived message)
        {
            if (this.State == SagaState.AwaitingReservationConfirmation)
            {
                this.State = SagaState.Completed;
                this.commands.Add(new CommitSeatReservation { ReservationId = message.OrderId, ConferenceId = message.ConferenceId });
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}

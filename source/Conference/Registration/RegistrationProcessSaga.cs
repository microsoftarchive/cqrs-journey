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
	using System.Linq;
	using Registration.Commands;
	using Common;
	using System.Collections.Generic;
	using Registration.Events;

	public class RegistrationProcessSaga : IAggregateRoot, ICommandPublisher
	{
		public enum SagaState
		{
			NotStarted = 0,
			AwaitingReservationConfirmation,
			AwaitingPayment,
			Completed = 0xFF,
		}

		private List<ICommand> commands = new List<ICommand>();

		public Guid Id { get; set; }

		public SagaState State { get; set; }

		public IEnumerable<ICommand> Commands
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
					new DelayCommand
						{
							SendDelay = TimeSpan.FromMinutes(15),
							Command = new ExpireSeatReservation { Id = message.ReservationId }
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

		public void Handle(ExpireSeatReservation message)
		{
			if (this.State == SagaState.AwaitingPayment)
			{
				this.State = SagaState.Completed;
				this.commands.Add(new RejectOrder { OrderId = message.Id });
			}

			// else ignore the message as it is no longer relevant (but not invalid)
		}
	}
}

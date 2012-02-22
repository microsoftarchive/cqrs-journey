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

	public class ConferenceSeatsAvailability : IAggregateRoot
	{
		private readonly Dictionary<Guid, int> pendingReservations;
		private int remainingSeats;

		// ORM requirement
		protected ConferenceSeatsAvailability() { }

		public ConferenceSeatsAvailability(Guid id)
		{
			this.Id = id;
			this.pendingReservations = new Dictionary<Guid, int>();
		}

		public Guid Id { get; private set; }

		public void AddSeats(int additionalSeats)
		{
			this.remainingSeats += additionalSeats;
		}

		public void MakeReservation(Guid reservationId, int numberOfSeats)
		{
			if (numberOfSeats > this.remainingSeats)
			{
				throw new ArgumentOutOfRangeException("numberOfSeats");
			}

			this.pendingReservations.Add(reservationId, numberOfSeats);
			this.remainingSeats -= numberOfSeats;
		}

		public void CommitReservation(Guid reservationId)
		{
			var numberOfSeats = this.pendingReservations[reservationId];
			this.pendingReservations.Remove(reservationId);
		}

		public void ExpireReservation(Guid reservationId)
		{
			var numberOfSeats = this.pendingReservations[reservationId];
			this.pendingReservations.Remove(reservationId);
			this.remainingSeats += numberOfSeats;
		}
	}
}

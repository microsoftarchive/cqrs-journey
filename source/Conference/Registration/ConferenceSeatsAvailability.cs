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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Registration
{
	public class ConferenceSeatsAvailability : IAggregateRoot
	{
		// ORM requirement
		protected ConferenceSeatsAvailability()
		{
			this.PendingReservations = new ObservableCollection<Reservation>();
		}

		public ConferenceSeatsAvailability(Guid id)
		{
			this.Id = id;
			this.PendingReservations = new ObservableCollection<Reservation>();
		}

		public virtual Guid Id { get; private set; }
		public virtual int RemainingSeats { get; set; }
		public virtual ObservableCollection<Reservation> PendingReservations { get; private set; }

		public void AddSeats(int additionalSeats)
		{
			this.RemainingSeats += additionalSeats;
		}

		public void MakeReservation(Guid reservationId, int numberOfSeats)
		{
			if (numberOfSeats > this.RemainingSeats)
			{
				throw new ArgumentOutOfRangeException("numberOfSeats");
			}

			this.PendingReservations.Add(new Reservation(reservationId, numberOfSeats));
			this.RemainingSeats -= numberOfSeats;
		}

		public void CommitReservation(Guid reservationId)
		{
			var reservation = this.PendingReservations.FirstOrDefault(r => r.Id == reservationId);
			if (reservation == null)
				throw new KeyNotFoundException();

			this.PendingReservations.Remove(reservation);
		}

		public void ExpireReservation(Guid reservationId)
		{
			var reservation = this.PendingReservations.FirstOrDefault(r => r.Id == reservationId);
			if (reservation == null)
				throw new KeyNotFoundException();

			this.PendingReservations.Remove(reservation);
			this.RemainingSeats += reservation.Seats;
		}
	}
}

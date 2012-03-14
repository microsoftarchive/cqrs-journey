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

namespace Registration.Tests.ConferenceSeatsAvailabilityFixture
{
	using System;
	using System.Collections.Generic;
    using System.Linq;
    using Xunit;
	using Registration.Database;
	using Common;
    using Registration.Events;

	public class given_available_seats
	{
		private static readonly Guid TicketTypeId = Guid.NewGuid();

		private ConferenceSeatsAvailability sut;
		private IPersistenceProvider sutProvider;

		protected given_available_seats(IPersistenceProvider sutProvider)
		{
			this.sutProvider = sutProvider;
			this.sut = new ConferenceSeatsAvailability(TicketTypeId);
			this.sut.AddSeats(10);

			this.sut = this.sutProvider.PersistReload(this.sut);
		}

		public given_available_seats()
			: this(new NoPersistenceProvider())
		{
		}

		[Fact]
		public void when_reserving_less_seats_than_total_then_seats_become_unavailable()
		{
			this.sut.MakeReservation(Guid.NewGuid(), 4);
			this.sut = this.sutProvider.PersistReload(this.sut);

			Assert.Equal(6, this.sut.RemainingSeats);
		}

		[Fact]
		public void when_reserving_more_seats_than_total_then_rejects()
		{
            var id = Guid.NewGuid();
            sut.MakeReservation(id, 11);

            Assert.Equal(1, sut.Events.Count());
            Assert.Equal(id, ((ReservationRejected)sut.Events.Single()).ReservationId);
		}
	}

	public class given_some_avilable_seats_and_some_taken
	{
		private static readonly Guid TicketTypeId = Guid.NewGuid();
		private static readonly Guid ReservationId = Guid.NewGuid();

		private ConferenceSeatsAvailability sut;
		private IPersistenceProvider sutProvider;

		protected given_some_avilable_seats_and_some_taken(IPersistenceProvider sutProvider)
		{
			this.sutProvider = sutProvider;
			this.sut = new ConferenceSeatsAvailability(TicketTypeId);
			this.sut.AddSeats(10);
			this.sut.MakeReservation(ReservationId, 6);

			this.sut = this.sutProvider.PersistReload(this.sut);
		}

		public given_some_avilable_seats_and_some_taken()
			: this(new NoPersistenceProvider())
		{
		}

		[Fact]
		public void when_reserving_less_seats_than_remaining_then_seats_become_unavailable()
		{
			this.sut.MakeReservation(Guid.NewGuid(), 4);
			this.sut = this.sutProvider.PersistReload(this.sut);

			Assert.Equal(0, sut.RemainingSeats);
		}

		[Fact]
		public void when_reserving_more_seats_than_remaining_then_rejects()
		{
            var id = Guid.NewGuid();
            sut.MakeReservation(id, 5);

            Assert.Equal(id, ((ReservationRejected)sut.Events.Last()).ReservationId);
		}

		[Fact]
		public void when_cancelling_a_reservation_then_seats_become_available()
		{
			this.sut.CancelReservation(ReservationId);
			this.sut = this.sutProvider.PersistReload(this.sut);

			Assert.Equal(10, sut.RemainingSeats);
		}

		[Fact]
        public void when_cancelling_an_inexistant_reservation_then_fails()
		{
			Assert.Throws<KeyNotFoundException>(() => sut.CancelReservation(Guid.NewGuid()));
		}

		[Fact]
		public void when_committing_a_reservation_then_remaining_seats_are_not_modified()
		{
			var remaining = this.sut.RemainingSeats;

			this.sut.CommitReservation(ReservationId);
			this.sut = this.sutProvider.PersistReload(this.sut);

			Assert.Equal(remaining, this.sut.RemainingSeats);
		}

		[Fact]
		public void when_committing_an_inexistant_reservation_then_fails()
		{
			Assert.Throws<KeyNotFoundException>(() => sut.CommitReservation(Guid.NewGuid()));
		}

		[Fact]
		public void when_committing_a_reservation_then_cannot_expire_it()
		{
			this.sut.CommitReservation(ReservationId);
			this.sut = this.sutProvider.PersistReload(this.sut);

			Assert.Throws<KeyNotFoundException>(() => sut.CancelReservation(ReservationId));
		}
	}
}

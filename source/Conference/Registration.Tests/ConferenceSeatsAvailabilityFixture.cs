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
using Xunit;

namespace Registration.Tests
{
    public class given_available_seats
    {
        protected static readonly Guid TicketTypeId = Guid.NewGuid();

        protected ConferenceSeatsAvailability sut;

        public given_available_seats()
        {
            this.sut = new ConferenceSeatsAvailability(TicketTypeId);
            this.sut.AddSeats(10);
        }

        [Fact]
        public void when_reserving_less_seats_than_total_then_seats_become_unavailable()
        {
            sut.MakeReservation(Guid.NewGuid(), 4);

            Assert.Equal(6, this.sut.RemainingSeats);
        }

        [Fact]
        public void when_reserving_more_seats_than_total_then_fails()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => sut.MakeReservation(Guid.NewGuid(), 11));
        }
    }

    public class given_some_avilable_seats_and_some_taken
    {
        protected static readonly Guid TicketTypeId = Guid.NewGuid();
        protected static readonly Guid ReservationId = Guid.NewGuid();

        protected ConferenceSeatsAvailability sut;

        public given_some_avilable_seats_and_some_taken()
        {
            this.sut = new ConferenceSeatsAvailability(TicketTypeId);
            this.sut.AddSeats(10); 
            this.sut.MakeReservation(ReservationId, 6);
        }

        [Fact]
        public void when_reserving_less_seats_than_remaining_then_seats_become_unavailable()
        {
            sut.MakeReservation(Guid.NewGuid(), 4);

            Assert.Equal(0, sut.RemainingSeats);
        }

        [Fact]
        public void when_reserving_more_seats_than_remaining_then_fails()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => sut.MakeReservation(Guid.NewGuid(), 5));
        }

        [Fact]
        public void when_expiring_a_reservation_then_seats_become_available()
        {
            sut.ExpireReservation(ReservationId);

            Assert.Equal(10, sut.RemainingSeats);
        }

        [Fact]
        public void when_expiring_an_inexistant_reservation_then_fails()
        {
            Assert.Throws<KeyNotFoundException>(() => sut.ExpireReservation(Guid.NewGuid()));
        }

        [Fact]
        public void when_committing_a_reservation_then_remaining_seats_are_not_modified()
        {
            var remaining = sut.RemainingSeats;

            sut.CommitReservation(ReservationId);

            Assert.Equal(remaining, sut.RemainingSeats);
        }

        [Fact]
        public void when_committing_an_inexistant_reservation_then_fails()
        {
            Assert.Throws<KeyNotFoundException>(() => sut.CommitReservation(Guid.NewGuid()));
        }

        [Fact]
        public void when_committing_a_reservation_then_cannot_expire_it()
        {
            sut.CommitReservation(ReservationId);

            Assert.Throws<KeyNotFoundException>(() => sut.ExpireReservation(ReservationId));
        }
    }
}

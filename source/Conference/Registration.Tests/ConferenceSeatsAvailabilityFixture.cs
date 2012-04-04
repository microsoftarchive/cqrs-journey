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
    using System.Linq;
    using Registration.Events;
    using Xunit;

    public class given_available_seats
    {
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private SeatsAvailability sut;
        private IPersistenceProvider sutProvider;

        protected given_available_seats(IPersistenceProvider sutProvider)
        {
            this.sutProvider = sutProvider;
            this.sut = new SeatsAvailability(SeatTypeId);
            this.sut.AddSeats(SeatTypeId, 10);

            this.sut = this.sutProvider.PersistReload(this.sut);
        }

        public given_available_seats()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_adding_non_existing_seat_type_then_adds_availability()
        {
            var seatType = Guid.NewGuid();
            this.sut.AddSeats(seatType, 50);

            this.sut = this.sutProvider.PersistReload(this.sut);
            Assert.Equal(50, this.sut.Seats.Single(x => x.SeatType == seatType).RemainingSeats);
        }

        [Fact]
        public void when_adding_seats_to_existing_seat_type_then_adds_remaining_seats()
        {
            this.sut.AddSeats(SeatTypeId, 10);

            this.sut = this.sutProvider.PersistReload(this.sut);
            Assert.Equal(20, this.sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_reserving_less_seats_than_total_then_reserves_all_requested_seats()
        {
            this.sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(SeatTypeId, 4) });

            Assert.Equal(1, sut.Events.Count());
            var e = this.sut.Events.OfType<SeatsReserved>().First();
            Assert.Equal(4, e.Seats[0].Quantity);

            // This state checking could be left only to the ORM integration test?
            this.sut = this.sutProvider.PersistReload(this.sut);
            Assert.Equal(6, this.sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_reserving_more_seats_than_total_then_reserves_total()
        {
            var id = Guid.NewGuid();
            sut.MakeReservation(id, new[] { new SeatQuantity(SeatTypeId, 11) });

            var e = this.sut.Events.OfType<SeatsReserved>().Last();

            Assert.Equal(id, e.ReservationId);
            Assert.Equal(SeatTypeId, e.Seats[0].SeatType);
            Assert.Equal(10, e.Seats[0].Quantity);
        }

        [Fact]
        public void when_reserving_non_existing_seat_type_then_throws()
        {
            var id = Guid.NewGuid();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.MakeReservation(id, new[] 
            { 
                new SeatQuantity(SeatTypeId, 11),
                new SeatQuantity(Guid.NewGuid(), 3),
            }));
        }
    }

    public class given_some_avilable_seats_and_some_taken
    {
        private setup_existing_reservation setup;

        protected given_some_avilable_seats_and_some_taken(IPersistenceProvider sutProvider)
        {
            this.setup = new setup_existing_reservation(sutProvider);
        }

        public given_some_avilable_seats_and_some_taken()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_reserving_less_seats_than_remaining_then_seats_become_unavailable()
        {
            this.setup.Sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(setup.SeatTypeId, 4) });
            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);

            Assert.Equal(0, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_reserving_more_seats_than_remaining_then_reserves_all_remaining()
        {
            var id = Guid.NewGuid();
            this.setup.Sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(this.setup.SeatTypeId, 5) });

            Assert.Equal(0, this.setup.Sut.Seats[0].RemainingSeats);
            Assert.Equal(4, this.setup.Sut.Events.OfType<SeatsReserved>().Last().Seats[0].Quantity);
        }

        [Fact]
        public void when_cancelling_an_inexistent_reservation_then_no_op()
        {
            this.setup.Sut.CancelReservation(Guid.NewGuid());
        }

        [Fact]
        public void when_committing_an_inexistant_reservation_then_no_op()
        {
            this.setup.Sut.CommitReservation(Guid.NewGuid());
        }
    }

    public class given_an_existing_reservation
    {
        private setup_existing_reservation setup;

        protected given_an_existing_reservation(IPersistenceProvider sutProvider)
        {
            this.setup = new setup_existing_reservation(sutProvider);
        }

        public given_an_existing_reservation()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_committed_then_remaining_seats_are_not_modified()
        {
            var remaining = this.setup.Sut.Seats[0].RemainingSeats;

            this.setup.Sut.CommitReservation(this.setup.ReservationId);
            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);

            Assert.Equal(remaining, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_cancelled_then_seats_become_available()
        {
            this.setup.Sut.CancelReservation(this.setup.ReservationId);
            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);

            Assert.Equal(10, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_committed_then_cannot_cancel_it()
        {
            var remaining = this.setup.Sut.Seats[0].RemainingSeats;

            this.setup.Sut.CommitReservation(this.setup.ReservationId);
            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);

            this.setup.Sut.CancelReservation(this.setup.ReservationId);

            Assert.Equal(remaining, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_updating_reservation_with_more_seats_then_reserves_all_requested()
        {
            this.setup.Sut.MakeReservation(this.setup.ReservationId, new[] 
            { 
                new SeatQuantity
                {
                    SeatType = this.setup.SeatTypeId, 
                    Quantity = 8,
                }
            });

            var e = this.setup.Sut.Events.OfType<SeatsReserved>().Last();

            Assert.Equal(this.setup.ReservationId, e.ReservationId);
            Assert.Equal(this.setup.SeatTypeId, e.Seats[0].SeatType);
            Assert.Equal(8, e.Seats[0].Quantity);

            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);
            Assert.Equal(2, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_updating_reservation_with_less_seats_then_updates_remaining()
        {
            this.setup.Sut.MakeReservation(this.setup.ReservationId, new[] { new SeatQuantity(this.setup.SeatTypeId, 2) });

            var e = this.setup.Sut.Events.OfType<SeatsReserved>().Last();

            Assert.Equal(this.setup.ReservationId, e.ReservationId);
            Assert.Equal(this.setup.SeatTypeId, e.Seats[0].SeatType);
            Assert.Equal(2, e.Seats[0].Quantity);

            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);
            Assert.Equal(8, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_updating_reservation_with_more_seats_than_available_then_reserves_as_much_as_possible()
        {
            this.setup.Sut.MakeReservation(this.setup.ReservationId, new[] 
            { 
                new SeatQuantity
                {
                    SeatType = this.setup.SeatTypeId, 
                    Quantity = 12,
                }
            });

            var e = this.setup.Sut.Events.OfType<SeatsReserved>().Last();

            Assert.Equal(this.setup.ReservationId, e.ReservationId);
            Assert.Equal(this.setup.SeatTypeId, e.Seats[0].SeatType);
            Assert.Equal(10, e.Seats[0].Quantity);

            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);
            Assert.Equal(0, this.setup.Sut.Seats[0].RemainingSeats);
        }

        [Fact]
        public void when_updating_reservation_with_different_seats_then_unreserves_the_previous_ones()
        {
            this.setup.Sut.MakeReservation(this.setup.ReservationId, new[] 
            { 
                new SeatQuantity
                {
                    SeatType = this.setup.OtherSeatTypeId, 
                    Quantity = 3,
                }
            });

            var e = this.setup.Sut.Events.OfType<SeatsReserved>().Last();

            Assert.Equal(this.setup.ReservationId, e.ReservationId);
            Assert.Equal(this.setup.OtherSeatTypeId, e.Seats[0].SeatType);
            Assert.Equal(3, e.Seats[0].Quantity);

            this.setup.Sut = this.setup.Persistence.PersistReload(this.setup.Sut);
            Assert.Equal(10, this.setup.Sut.Seats[0].RemainingSeats);
            Assert.Equal(9, this.setup.Sut.Seats[1].RemainingSeats);
        }
    }

    /// <summary>
    /// Sets up the seats availability so that 
    /// <see cref="SeatTypeId"/> has 10 initial 
    /// available seats, and reservation with 
    /// <see cref="ReservationId"/> is made for 
    /// 6 seats, leaving 4 remaining seats.
    /// </summary>
    public class setup_existing_reservation
    {
        public readonly Guid SeatTypeId = Guid.NewGuid();
        public readonly Guid OtherSeatTypeId = Guid.NewGuid();
        public readonly Guid ReservationId = Guid.NewGuid();

        public SeatsAvailability Sut { get; set; }
        public IPersistenceProvider Persistence { get; private set; }

        public setup_existing_reservation(IPersistenceProvider persistence)
        {
            this.Persistence = persistence;
            this.Sut = new SeatsAvailability(Guid.NewGuid());
            this.Sut.AddSeats(SeatTypeId, 10);
            this.Sut.AddSeats(OtherSeatTypeId, 12);
            this.Sut.MakeReservation(ReservationId, new[] { new SeatQuantity(SeatTypeId, 6) });

            this.Sut = this.Persistence.PersistReload(this.Sut);
        }
    }
}

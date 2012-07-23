// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
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
    using Infrastructure.EventSourcing;
    using Registration.Events;
    using Xunit;

    public class given
    {
        [Fact]
        public void when_adding_seat_type_then_changes_availability()
        {
            var id = Guid.NewGuid();
            var seatType = Guid.NewGuid();

            var sut = new SeatsAvailability(id);
            sut.AddSeats(seatType, 50);

            Assert.Equal(seatType, sut.SingleEvent<AvailableSeatsChanged>().Seats.Single().SeatType);
            Assert.Equal(50, sut.SingleEvent<AvailableSeatsChanged>().Seats.Single().Quantity);
        }
    }

    public class given_available_seats
    {
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private SeatsAvailability sut;

        public given_available_seats()
        {
            this.sut = new SeatsAvailability(ConferenceId, new[] { new AvailableSeatsChanged { Seats = new[] { new SeatQuantity(SeatTypeId, 10) } } });
        }

        [Fact]
        public void when_adding_non_existing_seat_type_then_adds_availability()
        {
            var seatType = Guid.NewGuid();
            sut.AddSeats(seatType, 50);

            Assert.Equal(seatType, sut.SingleEvent<AvailableSeatsChanged>().Seats.Single().SeatType);
            Assert.Equal(50, sut.SingleEvent<AvailableSeatsChanged>().Seats.Single().Quantity);
        }

        [Fact]
        public void when_adding_seats_to_existing_seat_type_then_adds_remaining_seats()
        {
            this.sut.AddSeats(SeatTypeId, 10);

            Assert.Equal(SeatTypeId, ((AvailableSeatsChanged)sut.Events.Single()).Seats.Single().SeatType);
            Assert.Equal(10, ((AvailableSeatsChanged)sut.Events.Single()).Seats.Single().Quantity);
        }

        [Fact]
        public void when_removing_seats_to_existing_seat_type_then_removes_remaining_seats()
        {
            this.sut.RemoveSeats(SeatTypeId, 5);

            this.sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(SeatTypeId, 10) });

            Assert.Equal(SeatTypeId, sut.Events.OfType<AvailableSeatsChanged>().Last().Seats.Single().SeatType);
            Assert.Equal(-5, sut.Events.OfType<AvailableSeatsChanged>().Last().Seats.Single().Quantity);
            Assert.Equal(5, this.sut.Events.OfType<SeatsReserved>().Single().ReservationDetails.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_reserving_less_seats_than_total_then_reserves_all_requested_seats()
        {
            this.sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(SeatTypeId, 4) });

            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.Equal(4, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_reserving_less_seats_than_total_then_reduces_remaining_seats()
        {
            this.sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(SeatTypeId, 4) });

            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.Equal(-4, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_reserving_more_seats_than_total_then_reserves_total()
        {
            var id = Guid.NewGuid();
            sut.MakeReservation(id, new[] { new SeatQuantity(SeatTypeId, 11) });

            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.Equal(10, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_reserving_more_seats_than_total_then_reduces_remaining_seats()
        {
            var id = Guid.NewGuid();
            sut.MakeReservation(id, new[] { new SeatQuantity(SeatTypeId, 11) });

            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.Equal(-10, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
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
        private SeatsAvailability sut;
        private Guid ConferenceId = Guid.NewGuid();
        private Guid SeatTypeId = Guid.NewGuid();
        private Guid OtherSeatTypeId = Guid.NewGuid();
        private Guid ReservationId = Guid.NewGuid();

        public given_some_avilable_seats_and_some_taken()
        {
            this.sut = new SeatsAvailability(ConferenceId, 
                new IVersionedEvent[]
                    {
                        new AvailableSeatsChanged
                            {
                                Seats = new[] { new SeatQuantity(SeatTypeId, 10) , new SeatQuantity(OtherSeatTypeId, 12) }
                            },
                        new SeatsReserved 
                        { 
                            ReservationId = ReservationId, 
                            ReservationDetails = new[] { new SeatQuantity(SeatTypeId, 6) }, 
                            AvailableSeatsChanged = new[] { new SeatQuantity(SeatTypeId, -6) }
                        }
                    });
        }

        [Fact]
        public void when_reserving_less_seats_than_remaining_then_seats_are_reserved()
        {
            sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(SeatTypeId, 4) });

            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.Equal(4, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.Equal(-4, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_reserving_more_seats_than_remaining_then_reserves_all_remaining()
        {
            var id = Guid.NewGuid();
            sut.MakeReservation(Guid.NewGuid(), new[] { new SeatQuantity(SeatTypeId, 5) });

            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).SeatType);
            Assert.Equal(4, this.sut.SingleEvent<SeatsReserved>().ReservationDetails.ElementAt(0).Quantity);
            Assert.Equal(SeatTypeId, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).SeatType);
            Assert.Equal(-4, this.sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_cancelling_an_inexistent_reservation_then_no_op()
        {
            sut.CancelReservation(Guid.NewGuid());

            Assert.Equal(0, sut.Events.Count());
        }

        [Fact]
        public void when_committing_an_inexistant_reservation_then_no_op()
        {
            sut.CommitReservation(Guid.NewGuid());

            Assert.Equal(0, sut.Events.Count());
        }
    }

    public class given_an_existing_reservation
    {
        private SeatsAvailability sut;
        private Guid ConferenceId = Guid.NewGuid();
        private Guid SeatTypeId = Guid.NewGuid();
        private Guid OtherSeatTypeId = Guid.NewGuid();
        private Guid ReservationId = Guid.NewGuid();

        public given_an_existing_reservation()
        {
            this.sut = new SeatsAvailability(
                ConferenceId,
                new IVersionedEvent[]
                    {
                        new AvailableSeatsChanged
                            {
                                Seats = new[] { new SeatQuantity(SeatTypeId, 10) , new SeatQuantity(OtherSeatTypeId, 12) },
                                Version = 1,
                            },
                        new SeatsReserved 
                        { 
                            ReservationId = ReservationId, 
                            ReservationDetails = new[] { new SeatQuantity(SeatTypeId, 6) }, 
                            AvailableSeatsChanged = new[] { new SeatQuantity(SeatTypeId, -6) },
                            Version = 2,
                        }
                    });
        }

        [Fact]
        public void when_committing_then_commits_reservation_id()
        {
            sut.CommitReservation(ReservationId);

            Assert.Equal(ReservationId, sut.SingleEvent<SeatsReservationCommitted>().ReservationId);
        }

        [Fact]
        public void when_cancelling_then_cancels_reservation_id()
        {
            sut.CancelReservation(ReservationId);

            Assert.Equal(ReservationId, sut.SingleEvent<SeatsReservationCancelled>().ReservationId);
        }

        [Fact]
        public void when_cancelled_then_seats_become_available()
        {
            sut.CancelReservation(ReservationId);

            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReservationCancelled>().AvailableSeatsChanged.Single().SeatType);
            Assert.Equal(6, sut.SingleEvent<SeatsReservationCancelled>().AvailableSeatsChanged.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_more_seats_then_reserves_all_requested()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(SeatTypeId, 8) });

            Assert.Equal(ReservationId, sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.Equal(8, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_more_seats_then_changes_available_seats()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(SeatTypeId, 8) });

            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().SeatType);
            Assert.Equal(-2, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_less_seats_then_reserves_all_requested()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(SeatTypeId, 2) });

            Assert.Equal(ReservationId, sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.Equal(2, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_less_seats_then_changes_available_seats()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(SeatTypeId, 2) });

            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().SeatType);
            Assert.Equal(4, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_more_seats_than_available_then_reserves_as_much_as_possible()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(SeatTypeId, 12) });

            Assert.Equal(ReservationId, sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.Equal(10, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);

            Assert.Equal(SeatTypeId, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().SeatType);
            Assert.Equal(-4, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_different_seats_then_reserves_them()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(OtherSeatTypeId, 3) });

            Assert.Equal(ReservationId, sut.SingleEvent<SeatsReserved>().ReservationId);
            Assert.Equal(OtherSeatTypeId, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().SeatType);
            Assert.Equal(3, sut.SingleEvent<SeatsReserved>().ReservationDetails.Single().Quantity);
        }

        [Fact]
        public void when_updating_reservation_with_different_seats_then_unreserves_the_previous_ones_and_reserves_new_ones()
        {
            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(OtherSeatTypeId, 3) });

            Assert.Equal(2, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Count());
            Assert.Equal(-3, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x => x.SeatType == OtherSeatTypeId).Quantity);
            Assert.Equal(6, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x => x.SeatType == SeatTypeId).Quantity);
        }

        [Fact]
        public void when_regenerating_from_memento_then_can_continue()
        {
            var memento = sut.SaveToMemento();
            sut = new SeatsAvailability(sut.Id, memento, Enumerable.Empty<IVersionedEvent>());

            Assert.Equal(2, sut.Version);

            sut.MakeReservation(ReservationId, new[] { new SeatQuantity(OtherSeatTypeId, 3) });

            Assert.Equal(2, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Count());
            Assert.Equal(-3, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x => x.SeatType == OtherSeatTypeId).Quantity);
            Assert.Equal(6, sut.SingleEvent<SeatsReserved>().AvailableSeatsChanged.Single(x => x.SeatType == SeatTypeId).Quantity);
            Assert.Equal(3, sut.SingleEvent<SeatsReserved>().Version);
        }
    }

    //public class given_a_cancelled_reservation
    //{
    //    private SeatsAvailability sut;
    //    private Guid ConferenceId = Guid.NewGuid();
    //    private Guid SeatTypeId = Guid.NewGuid();
    //    private Guid ReservationId = Guid.NewGuid();

    //    public given_a_cancelled_reservation()
    //    {
    //        this.sut = new SeatsAvailability(
    //            new IEvent[]
    //                {
    //                    new AvailableSeatsChanged(ConferenceId, new[] { new SeatQuantity(SeatTypeId, 10) }),
    //                    new SeatsReserved(ConferenceId, ReservationId, new[] {new SeatQuantity(SeatTypeId, 6)}, new[] {new SeatQuantity(SeatTypeId, -6)}),
    //                    new SeatsReservationCancelled(ConferenceId, ReservationId, new[] { new SeatQuantity(SeatTypeId, 6) })
    //                });
    //    }

    //    [Fact]
    //    public void cannot_commit_it()
    //    {
    //        Assert.Throws<ArgumentOutOfRangeException>(() => sut.CommitReservation(ReservationId));
    //    }
    //}

    //public class given_a_committed_reservation
    //{
    //    private SeatsAvailability sut;
    //    private Guid ConferenceId = Guid.NewGuid();
    //    private Guid SeatTypeId = Guid.NewGuid();
    //    private Guid ReservationId = Guid.NewGuid();

    //    public given_a_committed_reservation()
    //    {
    //        this.sut = new SeatsAvailability(
    //            new IEvent[]
    //                {
    //                    new AvailableSeatsChanged(ConferenceId, new[] { new SeatQuantity(SeatTypeId, 10) }),
    //                    new SeatsReserved(ConferenceId, ReservationId, new[] {new SeatQuantity(SeatTypeId, 6)}, new[] {new SeatQuantity(SeatTypeId, -6)}),
    //                    new SeatsReservationCommitted(ConferenceId, ReservationId)
    //                });
    //    }

    //    [Fact]
    //    public void cannot_cancel_it()
    //    {
    //        Assert.Throws<ArgumentOutOfRangeException>(() => sut.CancelReservation(ReservationId));
    //    }
    //}
}

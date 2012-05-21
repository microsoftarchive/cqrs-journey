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

namespace Registration.IntegrationTests.PricedOrderViewModelGeneratorFixture
{
    using System;
    using System.Linq;
    using Conference;
    using Infrastructure.Serialization;
    using Registration.Events;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Xunit;

    public class given_a_read_model_generator : given_a_read_model_database
    {
        protected PricedOrderViewModelGenerator sut;
        private IOrderDao dao;

        public given_a_read_model_generator()
        {
            this.sut = new PricedOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(dbName));
            this.dao = new OrderDao(() => new ConferenceRegistrationDbContext(dbName), new MemoryBlobStorage(), new JsonTextSerializer());
        }

        public class given_some_initial_seats : given_a_read_model_generator
        {
            protected SeatCreated[] seatCreatedEvents;
            public given_some_initial_seats()
            {
                this.seatCreatedEvents = new[]
                                     {
                                         new SeatCreated { SourceId = Guid.NewGuid(), Name = "General" },
                                         new SeatCreated { SourceId = Guid.NewGuid(), Name = "Precon" }
                                     };
                this.sut.Handle(this.seatCreatedEvents[0]);
                this.sut.Handle(this.seatCreatedEvents[1]);
            }
        }

        public class given_a_calculated_order : given_some_initial_seats
        {
            private static readonly Guid orderId = Guid.NewGuid();

            private PricedOrder dto;

            public given_a_calculated_order()
            {
                this.sut.Handle(new OrderTotalsCalculated
                {
                    SourceId = orderId,
                    Lines = new[]
                    {
                        new SeatOrderLine 
                        { 
                            LineTotal = 50, 
                            SeatType = this.seatCreatedEvents[0].SourceId, 
                            Quantity = 10, 
                            UnitPrice = 5 
                        },
                    },
                    Total = 50,
                    IsFreeOfCharge = true
                });

                this.dto = this.dao.FindPricedOrder(orderId);
            }

            [Fact]
            public void then_creates_model()
            {
                Assert.NotNull(dto);
            }

            [Fact]
            public void then_creates_order_lines()
            {
                Assert.Equal(1, dto.Lines.Count);
                Assert.Equal(50, dto.Lines[0].LineTotal);
                Assert.Equal(10, dto.Lines[0].Quantity);
                Assert.Equal(5, dto.Lines[0].UnitPrice);
                Assert.Equal(50, dto.Total);
            }

            [Fact]
            public void then_populates_description()
            {
                Assert.Equal("General", dto.Lines[0].Description);
            }

            [Fact]
            public void then_populates_is_free_of_charge()
            {
                Assert.Equal(true, dto.IsFreeOfCharge);
            }

            [Fact]
            public void when_recalculated_then_replaces_line()
            {
                this.sut.Handle(new OrderTotalsCalculated
                {
                    SourceId = orderId,
                    Lines = new[]
                    {
                        new SeatOrderLine 
                        { 
                            LineTotal = 20, 
                            SeatType = this.seatCreatedEvents[1].SourceId, 
                            Quantity = 2, 
                            UnitPrice = 10 
                        },
                    },
                    Total = 20,
                });

                var dto = this.dao.FindPricedOrder(orderId);

                Assert.Equal(1, dto.Lines.Count);
                Assert.Equal(20, dto.Lines[0].LineTotal);
                Assert.Equal(2, dto.Lines[0].Quantity);
                Assert.Equal(10, dto.Lines[0].UnitPrice);
                Assert.Equal(20, dto.Total);
                Assert.Equal("Precon", dto.Lines[0].Description);
            }

            [Fact]
            public void when_expired_then_deletes_priced_order()
            {
                this.sut.Handle(new OrderExpired { SourceId = orderId });

                var dto = this.dao.FindPricedOrder(orderId);

                Assert.Null(dto);
            }

            [Fact]
            public void when_seat_assignments_created_then_updates_order_with_assignments_id()
            {
                var assignmentsId = Guid.NewGuid();
                this.sut.Handle(new SeatAssignmentsCreated { SourceId = assignmentsId, OrderId = orderId });

                var dto = this.dao.FindPricedOrder(orderId);

                Assert.Equal(assignmentsId, dto.AssignmentsId);
            }
        }

        public class given_changes_to_seats : given_some_initial_seats
        {
            protected SeatUpdated[] seatUpdatedEvents;
            public given_changes_to_seats()
            {
                this.seatUpdatedEvents = new[]
                                     {
                                         new SeatUpdated { SourceId = seatCreatedEvents[0].SourceId, Name = "General_Updated" },
                                         new SeatUpdated { SourceId = seatCreatedEvents[1].SourceId, Name = "Precon_Updated" },
                                     };
                this.sut.Handle(this.seatUpdatedEvents[0]);
            }
        }

        public class given_a_new_calculated_order : given_changes_to_seats
        {
            private static readonly Guid orderId = Guid.NewGuid();

            private PricedOrder dto;

            public given_a_new_calculated_order()
            {
                this.sut.Handle(new OrderTotalsCalculated
                {
                    SourceId = orderId,
                    Lines = new[]
                    {
                        new SeatOrderLine 
                        { 
                            LineTotal = 50, 
                            SeatType = this.seatCreatedEvents[0].SourceId, 
                            Quantity = 10, 
                            UnitPrice = 5 
                        },
                        new SeatOrderLine 
                        { 
                            LineTotal = 10, 
                            SeatType = this.seatCreatedEvents[1].SourceId, 
                            Quantity = 1, 
                            UnitPrice = 10 
                        },
                    },
                    Total = 60,
                    IsFreeOfCharge = true
                });

                this.dto = this.dao.FindPricedOrder(orderId);
            }

            [Fact]
            public void then_populates_with_updated_description()
            {
                Assert.True(dto.Lines.Any(x => x.Description == "General_Updated"));
                Assert.True(dto.Lines.Any(x => x.Description == "Precon"));
            }

            [Fact]
            public void when_recalculated__after_new_update_then_replaces_line()
            {
                this.sut.Handle(seatUpdatedEvents[1]);
                this.sut.Handle(new OrderTotalsCalculated
                {
                    SourceId = orderId,
                    Lines = new[]
                    {
                        new SeatOrderLine 
                        { 
                            LineTotal = 10, 
                            SeatType = this.seatCreatedEvents[0].SourceId, 
                            Quantity = 2, 
                            UnitPrice = 5 
                        },
                        new SeatOrderLine 
                        { 
                            LineTotal = 20, 
                            SeatType = this.seatCreatedEvents[1].SourceId, 
                            Quantity = 2, 
                            UnitPrice = 10 
                        },
                    },
                    Total = 30,
                });

                var dto = this.dao.FindPricedOrder(orderId);

                Assert.True(dto.Lines.Any(x => x.Description == "General_Updated"));
                Assert.True(dto.Lines.Any(x => x.Description == "Precon_Updated"));
            }
        }
    }
}

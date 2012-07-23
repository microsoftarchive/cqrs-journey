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

namespace Registration.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Moq;
    using Registration.ReadModel;
    using Xunit;

    public class PricingServiceFixture
    {
        public static readonly Guid ConferenceId = Guid.NewGuid();
        private Mock<IConferenceDao> dao;
        private PricingService sut;
        private SeatType[] seatTypes;

        public PricingServiceFixture()
        {
            this.dao = new Mock<IConferenceDao>();
            this.seatTypes = new[]
                                {
                                    new SeatType(Guid.NewGuid(), ConferenceId, "Name1", "Desc1", 15.10m, 999),
                                    new SeatType(Guid.NewGuid(), ConferenceId, "Name2", "Desc2", 9.987m, 600),
                                };
            dao.Setup(d => d.GetPublishedSeatTypes(ConferenceId)).Returns(seatTypes);

            this.sut = new PricingService(dao.Object);
        }

        [Fact]
        public void when_passing_valid_seat_types_then_sums_individual_prices()
        {
            var actual = sut.CalculateTotal(ConferenceId, new[] { new SeatQuantity(seatTypes[0].Id, 3) });

            Assert.Equal(45.3m, actual.Total);
            Assert.Equal(1, actual.Lines.Count());
            Assert.Equal(45.3m, actual.Lines.ElementAt(0).LineTotal);
            Assert.Equal(15.1m, ((SeatOrderLine)actual.Lines.ElementAt(0)).UnitPrice);
            Assert.Equal(seatTypes[0].Id, ((SeatOrderLine)actual.Lines.ElementAt(0)).SeatType);
            Assert.Equal(3, ((SeatOrderLine)actual.Lines.ElementAt(0)).Quantity);
        }

        [Fact]
        public void when_passing_invalid_seat_types_then_throws()
        {
            Assert.Throws<InvalidDataException>(() => sut.CalculateTotal(ConferenceId, new[] { new SeatQuantity(Guid.NewGuid(), 3) }));
        }

        [Fact]
        public void rounds_to_near_2_digit_decimal()
        {
            var actual = sut.CalculateTotal(ConferenceId, new[] { new SeatQuantity(seatTypes[1].Id, 1) });

            Assert.Equal(9.99m, actual.Total);
            Assert.Equal(1, actual.Lines.Count());
            Assert.Equal(9.99m, actual.Lines.ElementAt(0).LineTotal);
            Assert.Equal(9.987m, ((SeatOrderLine)actual.Lines.ElementAt(0)).UnitPrice);
            Assert.Equal(seatTypes[1].Id, ((SeatOrderLine)actual.Lines.ElementAt(0)).SeatType);
            Assert.Equal(1, ((SeatOrderLine)actual.Lines.ElementAt(0)).Quantity);
        }
    }
}

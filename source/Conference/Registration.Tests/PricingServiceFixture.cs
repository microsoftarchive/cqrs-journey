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
        private ConferenceSeatTypeDTO[] seatTypes;

        public PricingServiceFixture()
        {
            this.dao = new Mock<IConferenceDao>();
            this.seatTypes = new[]
                                {
                                    new ConferenceSeatTypeDTO(Guid.NewGuid(), "Name1", "Desc1", 15m, 999),
                                    new ConferenceSeatTypeDTO(Guid.NewGuid(), "Name2", "Desc2", 9.97m, 600),
                                };
            dao.Setup(d => d.GetPublishedSeatTypes(ConferenceId)).Returns(seatTypes);

            this.sut = new PricingService(dao.Object);
        }

        [Fact]
        public void when_passing_valid_seat_types_then_sums_individual_prices()
        {
            var actual = sut.CalculateTotal(ConferenceId, new[] { new SeatQuantity(seatTypes[0].Id, 3) });

            Assert.Equal(45m, actual.Total);
            Assert.Equal(1, actual.Lines.Count());
            Assert.Equal(45m, actual.Lines.ElementAt(0).LineTotal);
            Assert.Equal(15m, ((SeatOrderLine)actual.Lines.ElementAt(0)).UnitPrice);
            Assert.Equal(seatTypes[0].Id, ((SeatOrderLine)actual.Lines.ElementAt(0)).SeatType);
        }

        [Fact]
        public void when_passing_invalid_seat_types_then_throws()
        {
            Assert.Throws<InvalidDataException>(() => sut.CalculateTotal(ConferenceId, new[] { new SeatQuantity(Guid.NewGuid(), 3) }));
        }
    }
}

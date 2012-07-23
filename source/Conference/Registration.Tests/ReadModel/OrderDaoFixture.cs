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

namespace Registration.Tests.ReadModel
{
    using System;
    using System.IO;
    using Infrastructure.BlobStorage;
    using Infrastructure.Serialization;
    using Moq;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Xunit;

    public class OrderDaoFixture
    {
        [Fact]
        public void when_finding_non_existing_assignment_then_returns_null()
        {
            var storage = new Mock<IBlobStorage>();
            storage.SetReturnsDefault<byte[]>(null);
            var dao = new OrderDao(() => new ConferenceRegistrationDbContext("OrderDaoFixture"), storage.Object, Mock.Of<ITextSerializer>());

            var dto = dao.FindOrderSeats(Guid.NewGuid());

            Assert.Null(dto);
        }

        [Fact]
        public void when_finding_existing_dao_then_deserializes_blob_and_returns_instance()
        {
            var dto = new OrderSeats();
            var storage = Mock.Of<IBlobStorage>(x => x.Find(It.IsAny<string>()) == new byte[0]);
            var serializer = Mock.Of<ITextSerializer>(x => x.Deserialize(It.IsAny<TextReader>()) == dto);
            var dao = new OrderDao(() => new ConferenceRegistrationDbContext("OrderDaoFixture"), storage, serializer);

            var result = dao.FindOrderSeats(Guid.NewGuid());

            Assert.Same(result, dto);
        }
    }
}

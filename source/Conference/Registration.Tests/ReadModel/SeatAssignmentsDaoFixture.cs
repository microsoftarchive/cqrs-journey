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

namespace Registration.Tests.ReadModel
{
    using System;
    using System.IO;
    using Infrastructure.Blob;
    using Infrastructure.Serialization;
    using Moq;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Xunit;

    public class SeatAssignmentsDaoFixture
    {
        [Fact]
        public void when_finding_non_existing_assignment_then_returns_null()
        {
            var storage = new Mock<IBlobStorage>();
            storage.SetReturnsDefault<byte[]>(null);
            var dao = new SeatAssignmentsDao(storage.Object, Mock.Of<ITextSerializer>());

            var dto = dao.Find(Guid.NewGuid());

            Assert.Null(dto);
        }

        [Fact]
        public void when_finding_existing_dao_then_deserializes_blob_and_returns_instance()
        {
            var dto = new SeatAssignmentsDTO();
            var storage = Mock.Of<IBlobStorage>(x => x.Find(It.IsAny<string>()) == new byte[0]);
            var serializer = Mock.Of<ITextSerializer>(x => x.Deserialize(It.IsAny<TextReader>()) == dto);
            var dao = new SeatAssignmentsDao(storage, serializer);

            var result = dao.Find(Guid.NewGuid());

            Assert.Same(result, dto);
        }
    }
}

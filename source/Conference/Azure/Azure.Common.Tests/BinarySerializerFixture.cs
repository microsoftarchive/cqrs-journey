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

namespace Azure.Tests
{
    using System;
    using System.IO;
    using Xunit;

    public class BinarySerializerFixture
    {
        [Fact]
        public void when_using_adapter_then_can_roundtrip_serialized_object()
        {
            var command = new Command
            {
                Id = 5,
                Title = "Foo",
            };

            var adapter = new BinarySerializer();
            using (var stream = new MemoryStream())
            {
                adapter.Serialize(stream, command);

                stream.Position = 0;

                var deserialized = (Command)adapter.Deserialize(stream, typeof(Command));

                Assert.Equal(command.Id, deserialized.Id);
                Assert.Equal(command.Title, deserialized.Title);
            }
        }

        [Serializable]
        public class Command
        {
            public int Id { get; set; }
            public string Title { get; set; }
        }
    }
}

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

namespace Infrastructure.Azure.Tests.Messaging
{
    using System;
    using System.Linq;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Tests.Mocks;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Moq;
    using Xunit;

    public class CommandBusFixture
    {
        [Fact]
        public void when_sending_then_sets_command_id_as_messageid()
        {
            var sender = new MessageSenderMock();
            var sut = new CommandBus(sender, Mock.Of<IMetadataProvider>(), new JsonTextSerializer());

            var command = new FooCommand { Id = Guid.NewGuid() };
            sut.Send(command);

            Assert.Equal(command.Id.ToString(), sender.Sent.Single().MessageId);
        }

        class FooCommand : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}

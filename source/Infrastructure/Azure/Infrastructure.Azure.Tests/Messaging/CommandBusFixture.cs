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

        [Fact]
        public void when_specifying_time_to_live_then_sets_in_message()
        {
            var sender = new MessageSenderMock();
            var sut = new CommandBus(sender, Mock.Of<IMetadataProvider>(), new JsonTextSerializer());

            var command = new Envelope<ICommand>(new FooCommand { Id = Guid.NewGuid() })
            {
                TimeToLive = TimeSpan.FromMinutes(15)
            };
            sut.Send(command);

            Assert.InRange(sender.Sent.Single().TimeToLive, TimeSpan.FromMinutes(14.9), TimeSpan.FromMinutes(15.1));
        }

        [Fact]
        public void when_specifying_delay_then_sets_in_message()
        {
            var sender = new MessageSenderMock();
            var sut = new CommandBus(sender, Mock.Of<IMetadataProvider>(), new JsonTextSerializer());

            var command = new Envelope<ICommand>(new FooCommand { Id = Guid.NewGuid() })
            {
                Delay = TimeSpan.FromMinutes(15)
            };
            sut.Send(command);

            Assert.InRange(sender.Sent.Single().ScheduledEnqueueTimeUtc, DateTime.UtcNow.AddMinutes(14.9), DateTime.UtcNow.AddMinutes(15.1));
        }

        class FooCommand : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}

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

namespace Infrastructure.Azure.Tests
{
    using System;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class MessageProcessorFixture
    {
        [Fact]
        public void when_starting_twice_then_ignores_second_request()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ITextSerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Start();

            processor.Start();
        }

        [Fact]
        public void when_disposing_started_then_stops()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ITextSerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Start();
            processor.Dispose();

            Mock.Get(processor).Verify(x => x.Stop());
        }

        [Fact]
        public void when_disposing_then_disposes_receiver_if_disposable()
        {
            var receiver = new Mock<IMessageReceiver>();
            var disposable = receiver.As<IDisposable>();
            var serializer = new Mock<ITextSerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Dispose();

            disposable.Verify(x => x.Dispose());
        }

        [Fact]
        public void when_stopping_disposed_then_ignores()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ITextSerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Dispose();

            processor.Stop();
        }

        [Fact]
        public void when_stopping_non_started_then_ignores()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ITextSerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Stop();
        }
    }
}

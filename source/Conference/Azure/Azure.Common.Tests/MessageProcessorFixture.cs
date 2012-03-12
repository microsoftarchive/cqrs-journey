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
    using Azure.Messaging;
    using Common;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class MessageProcessorFixture
    {
        [Fact]
        public void when_starting_disposed_then_throws()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;


            processor.Dispose();

            Assert.Throws<ObjectDisposedException>(() => processor.Start());
        }

        [Fact]
        public void when_starting_twice_then_throws()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Start();

            Assert.Throws<InvalidOperationException>(() => processor.Start());
        }

        [Fact]
        public void when_disposing_started_then_stops()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
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
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Dispose();

            disposable.Verify(x => x.Dispose());
        }

        [Fact]
        public void when_stopping_disposed_then_throws()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            processor.Dispose();

            Assert.Throws<ObjectDisposedException>(() => processor.Stop());
        }

        [Fact]
        public void when_stopping_non_started_then_throws()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            Assert.Throws<InvalidOperationException>(() => processor.Stop());
        }

        [Fact]
        public void when_message_received_without_type_then_does_not_call_process_message()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            var message = new BrokeredMessage("foo");

            receiver.Raise(x => x.MessageReceived += null, new BrokeredMessageEventArgs(message));

            Mock.Get(processor).Protected().Verify("ProcessMessage", Times.Never(), ItExpr.IsAny<object>(), ItExpr.IsAny<Type>());
        }

        [Fact]
        public void when_message_received_without_assembly_then_does_not_call_process_message()
        {
            var receiver = new Mock<IMessageReceiver>();
            var serializer = new Mock<ISerializer>();
            var processor = new Mock<MessageProcessor>(receiver.Object, serializer.Object) { CallBase = true }.Object;

            var message = new BrokeredMessage("foo");
            message.Properties["Type"] = typeof(IFormatProvider).FullName;

            receiver.Raise(x => x.MessageReceived += null, new BrokeredMessageEventArgs(message));

            Mock.Get(processor).Protected().Verify("ProcessMessage", Times.Never(), ItExpr.IsAny<object>(), ItExpr.IsAny<Type>());
        }
    }
}

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

namespace Infrastructure.Tests.Messaging.Handling.MessageDispatcherFixture
{
    using System;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Moq;
    using Xunit;

    public class given_empty_dispatcher
    {
        private EventDispatcher sut;

        public given_empty_dispatcher()
        {
            this.sut = new EventDispatcher();
        }

        [Fact]
        public void when_dispatching_an_event_then_does_nothing()
        {
            var @event = new EventC();

            this.sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }

    public class given_dispatcher_with_handler
    {
        private EventDispatcher sut;
        private Mock<IEventHandler> handlerMock;

        public given_dispatcher_with_handler()
        {
            this.sut = new EventDispatcher();

            this.handlerMock = new Mock<IEventHandler>();
            this.handlerMock.As<IEventHandler<EventA>>();

            this.sut.Register(this.handlerMock.Object);
        }

        [Fact]
        public void when_dispatching_an_event_with_registered_handler_then_invokes_handler()
        {
            var @event = new EventA();

            this.sut.DispatchMessage(@event, "message", "correlation", "");

            this.handlerMock.As<IEventHandler<EventA>>().Verify(h => h.Handle(@event), Times.Once());
        }

        [Fact]
        public void when_dispatching_an_event_with_no_registered_handler_then_does_nothing()
        {
            var @event = new EventC();

            this.sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }

    public class given_dispatcher_with_handler_for_envelope
    {
        private EventDispatcher sut;
        private Mock<IEventHandler> handlerMock;

        public given_dispatcher_with_handler_for_envelope()
        {
            this.sut = new EventDispatcher();

            this.handlerMock = new Mock<IEventHandler>();
            this.handlerMock.As<IEnvelopedEventHandler<EventA>>();

            this.sut.Register(this.handlerMock.Object);
        }

        [Fact]
        public void when_dispatching_an_event_with_registered_handler_then_invokes_handler()
        {
            var @event = new EventA();

            this.sut.DispatchMessage(@event, "message", "correlation", "");

            this.handlerMock.As<IEnvelopedEventHandler<EventA>>()
                .Verify(
                    h => h.Handle(It.Is<Envelope<EventA>>(e => e.Body == @event && e.MessageId == "message" && e.CorrelationId == "correlation")),
                    Times.Once());
        }

        [Fact]
        public void when_dispatching_an_event_with_no_registered_handler_then_does_nothing()
        {
            var @event = new EventC();

            this.sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }

    public class given_dispatcher_with_multiple_handlers
    {
        private EventDispatcher sut;
        private Mock<IEventHandler> handler1Mock;
        private Mock<IEventHandler> handler2Mock;

        public given_dispatcher_with_multiple_handlers()
        {
            this.sut = new EventDispatcher();

            this.handler1Mock = new Mock<IEventHandler>();
            this.handler1Mock.As<IEnvelopedEventHandler<EventA>>();
            this.handler1Mock.As<IEventHandler<EventB>>();

            this.sut.Register(this.handler1Mock.Object);

            this.handler2Mock = new Mock<IEventHandler>();
            this.handler2Mock.As<IEventHandler<EventA>>();

            this.sut.Register(this.handler2Mock.Object);
        }

        [Fact]
        public void when_dispatching_an_event_with_multiple_registered_handlers_then_invokes_handlers()
        {
            var @event = new EventA();

            this.sut.DispatchMessage(@event, "message", "correlation", "");

            this.handler1Mock.As<IEnvelopedEventHandler<EventA>>()
                .Verify(
                    h => h.Handle(It.Is<Envelope<EventA>>(e => e.Body == @event && e.MessageId == "message" && e.CorrelationId == "correlation")),
                    Times.Once());
            this.handler2Mock.As<IEventHandler<EventA>>().Verify(h => h.Handle(@event), Times.Once());
        }

        [Fact]
        public void when_dispatching_an_event_with_single_registered_handler_then_invokes_handler()
        {
            var @event = new EventB();

            this.sut.DispatchMessage(@event, "message", "correlation", "");

            this.handler1Mock.As<IEventHandler<EventB>>().Verify(h => h.Handle(@event), Times.Once());
        }

        [Fact]
        public void when_dispatching_an_event_with_no_registered_handler_then_does_nothing()
        {
            var @event = new EventC();

            this.sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }

    public class EventA : IEvent
    {
        public Guid SourceId
        {
            get { return Guid.Empty; }
        }
    }

    public class EventB : IEvent
    {
        public Guid SourceId
        {
            get { return Guid.Empty; }
        }
    }

    public class EventC : IEvent
    {
        public Guid SourceId
        {
            get { return Guid.Empty; }
        }
    }
}

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

namespace Registration.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Xunit;
	using Moq;
	using Common;
	using System.Threading;

	public class MemoryCommandBusFixture
	{
		[Fact]
		public void WhenSendingCommand_ThenInvokesCompatibleHandler()
		{
			var handler = new Mock<ICommandHandler<TestCommand>>();
			var called = false;
			handler.Setup(x => x.Handle(It.IsAny<TestCommand>()))
				.Callback(() => called = true);

			var bus = new MemoryCommandBus(handler.Object);

			bus.Send(new TestCommand());

			while (!called)
			{
				Thread.Sleep(10);
			}

			Assert.True(called);
		}

		[Fact]
		public void WhenSendingCommand_ThenDoesNotInvokeIncompatibleHandler()
		{
			var compatibleHandler = new Mock<ICommandHandler<TestCommand>>();
			var incompatibleHandler = new Mock<ICommandHandler<FooCommand>>();
			var e = new ManualResetEventSlim();

			compatibleHandler.Setup(x => x.Handle(It.IsAny<TestCommand>()))
				.Callback(() => e.Set());

			var bus = new MemoryCommandBus(incompatibleHandler.Object, compatibleHandler.Object);

			bus.Send(new TestCommand());

			e.Wait(1000);

			incompatibleHandler.Verify(x => x.Handle(It.IsAny<FooCommand>()), Times.Never());
		}

		[Fact]
		public void WhenSendingMultipleCommands_ThenInvokesCompatibleHandlerMultipleTimes()
		{
			var handler = new Mock<ICommandHandler<TestCommand>>();
			var e = new ManualResetEventSlim();
			
			var called = 0;
			handler.Setup(x => x.Handle(It.IsAny<TestCommand>()))
				.Callback(() => { if (called++ == 4) e.Set(); });

			var bus = new MemoryCommandBus(handler.Object);

			bus.Send(new [] { new TestCommand(), new TestCommand(), new TestCommand(), new TestCommand() });

			e.Wait(1000);

			Assert.Equal(4, called);
		}

		public class TestCommand : ICommand
		{
			public Guid Id { get; set; }
		}

		public class FooCommand : ICommand
		{
			public Guid Id { get; set; }
		}
	}
}

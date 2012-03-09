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
    using System.Threading;
    using Common;
    using Xunit;

    public class CommandsProcessingFixture
    {
        [Fact]
        public void WhenReceivingCommand_ThenCanCallHandlers()
        {
            var processor = new CommandProcessor();
            var bus = new CommandBus(new BusSettings());

            var e = new ManualResetEvent(false);
            var handler = new FooCommandHandler(e);

            processor.RegisterHandler(handler);

            processor.Start();

            try
            {
                bus.Send(new FooCommand());

                e.WaitOne(1000);

                if (!handler.Called)
                    Assert.False(true);
            }
            finally
            {
                processor.Stop();
            }
        }


        public class FooCommand : ICommand
        {
            public FooCommand()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }

        public class BarCommand : ICommand
        {
            public BarCommand()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }

        public class FooCommandHandler : ICommandHandler<FooCommand>
        {
            private ManualResetEvent e;

            public FooCommandHandler(ManualResetEvent e)
            {
                this.e = e;
            }

            public void Handle(FooCommand command)
            {
                e.Set();
                this.Called = true;
            }

            public bool Called { get; set; }
        }
    }
}

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

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;

    public class SynchronousCommandBusDecorator : ICommandBus, ICommandHandlerRegistry
    {
        private readonly ICommandBus commandBus;
        private readonly CommandDispatcher commandDispatcher;

        public SynchronousCommandBusDecorator(ICommandBus commandBus)
        {
            this.commandBus = commandBus;
            this.commandDispatcher = new CommandDispatcher();
        }

        public void Register(ICommandHandler commandHandler)
        {
            this.commandDispatcher.Register(commandHandler);
        }

        public void Send(Envelope<ICommand> command)
        {
            if (!this.DoSend(command))
            {
                // Trace.TraceInformation("Command with id {0} was not handled locally. Sending it through the bus.", command.Body.Id);
                this.commandBus.Send(command);
            }
        }

        public void Send(IEnumerable<Envelope<ICommand>> commands)
        {
            var pending = commands.ToList();

            while (pending.Count > 0)
            {
                if (this.DoSend(pending[0]))
                {
                    pending.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            if (pending.Count > 0)
            {
                // Trace.TraceInformation("Command with id {0} was not handled locally. Sending it and all remaining commands through the bus.", pending[0].Body.Id);
                this.commandBus.Send(pending);
            }
        }

        private bool DoSend(Envelope<ICommand> command)
        {
            bool handled = false;

            try
            {
                var traceIdentifier = string.Format(CultureInfo.CurrentCulture, " (local handling of command with id {0})", command.Body.Id);
                handled = this.commandDispatcher.ProcessMessage(traceIdentifier, command.Body, command.MessageId, command.CorrelationId);

                // TODO try to log the command
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Exception handling command with id {0} synchronously: {1}. Command will be sent through the bus.", command.Body.Id, e.Message);
            }

            return handled;
        }
    }
}

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

using System;
using System.Diagnostics;

namespace Infrastructure.Messaging
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides usability overloads for <see cref="ICommandBus"/>
    /// </summary>
    public static class MessagingExtensions
    {
        public static void Send(this ICommandBus bus, ICommand command)
        {
            bus.Send(new Envelope<ICommand>(command));
        }

        public static void Send(this ICommandBus bus, IEnumerable<ICommand> commands)
        {
            bus.Send(commands.Select(x => new Envelope<ICommand>(x)));
        }

        public static T TraceCommand<T>(this T command, Func<T, string> projection) where T : ICommand
        {
            Debug.WriteLine(FormatDebugText<T>(command, "TraceCommand"));
            return command;
        }

        public static string FormatDebugText<T>(this ICommand command, string source, Func<T, string> dataSelector = null) where T : ICommand
        {
            const string fmt = "{0}*** {1} Trace ***{0}*** {1} ***{0}";
            return string.Format(fmt, Environment.NewLine,
                                 string.Format("Command: {0}, Source: {1}, Data: {2}", command.GetType().Name, source, dataSelector == null ? "N/A" : dataSelector((T)command)));
        }

        public static string FormatDebugText<T>(this Envelope<ICommand> command) where T : ICommand
        {
            return FormatDebugText<T>(command.Body, command.Delay.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Infrastructure.Messaging;

namespace Common.Test
{
    public static class Extensions
    {
        public static T TraceCommand<T>(this T command, Func<T, string> projection) where T : ICommand
        {
            Debug.WriteLine(FormatDebugText<T>(command, "TraceCommand"));
            return command;
        }

        public static string FormatDebugText<T>(this ICommand command, string source, Func<T, string> dataSelector = null) where T : ICommand
        {
            const string fmt = "{0}*** {1} Trace ***{0}*** {1} ***{0}";
            return String.Format(fmt, Environment.NewLine,
                                 String.Format("Command: {0}, Source: {1}, Data: {2}", command.GetType().Name, source, dataSelector == null ? "N/A" : dataSelector((T)command)));
        }

        public static string FormatDebugText<T>(this Envelope<ICommand> command) where T : ICommand
        {
            return FormatDebugText<T>(command.Body, command.Delay.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace Conference.Specflow.Support
{
    [Binding]
    public class TestEvents
    {
        private static Process consoleInstance;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            // Check if the WorkerRoleCommandProcessor is running
            var start = new ProcessStartInfo
            {
                FileName = Path.GetFullPath(@"..\..\..\..\WorkerRoleCommandProcessor\bin\Debug\CommandProcessor.exe"),
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false
            };

            // Check if the WorkerRoleCommandProcessor is running
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(start.FileName)).Any())
            {
                return;
            }

            consoleInstance = Process.Start(start);
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            if(consoleInstance != null)
                consoleInstance.Dispose();
        }
    }
}

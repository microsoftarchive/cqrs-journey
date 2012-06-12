using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using TechTalk.SpecFlow;

namespace Conference.Specflow.Support
{
    [Binding]
    public class TestEvents
    {
        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            StartWebHost(42000, Path.GetFullPath(@"..\..\..\..\Conference\Conference.Web"));
            StartWebHost(43000, Path.GetFullPath(@"..\..\..\..\Conference\Conference.Web.Public"));

            // Check if the WorkerRoleCommandProcessor is running
            var start = new ProcessStartInfo
            {
                FileName = Path.GetFullPath(@"..\..\..\..\WorkerRoleCommandProcessor\bin\Debug\CommandProcessor.exe"),
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
            };

            // Check if the WorkerRoleCommandProcessor is running
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(start.FileName)).Any())
            {
                return;
            }

            // Start in a new thread to dec
            Process.Start(Path.GetFullPath(@"..\..\..\..\WorkerRoleCommandProcessor\bin\Debug\CommandProcessor.exe"));
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
        }

        private static void StartWebHost(int port, string projectPhysicalPath)
        {
            if (PortInUse(port))
                return;

            string hostPath = Path.Combine(GetCommonProgramFilesPath(), @"Microsoft Shared\DevServer\10.0\WebDev.WebServer40.exe");
            string hostArgs = string.Format(CultureInfo.InvariantCulture, "/port:{0} /nodirlist /path:\"{1}\" /vpath:\"{2}\"", port, projectPhysicalPath, "/");

            Process.Start(hostPath, hostArgs);
        }

        private static bool PortInUse(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            return ipGlobalProperties.GetActiveTcpListeners().Any(l => l.Port == port);
        }

        private static string GetCommonProgramFilesPath()
        {
            if(Environment.Is64BitOperatingSystem)
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86);

            return Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        }
    }
}

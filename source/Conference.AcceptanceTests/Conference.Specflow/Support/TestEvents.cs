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

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using TechTalk.SpecFlow;
using WatiN.Core;

namespace Conference.Specflow.Support
{
    [Binding]
    public class TestEvents
    {
        //[STAThread] //http://watin.org/documentation/sta-apartmentstate/
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

            // May be used to control browser visibility 
            // instead of setting Visible = true on each instance creation.
            //Settings.Instance.MakeNewIeInstanceVisible = true;

            // Close all running IE instances
            while (IE.InternetExplorersNoWait().Count > 0)
            {
                var ie = IE.InternetExplorersNoWait()[0];
                ie.ForceClose();
            } 


            // Check if the WorkerRoleCommandProcessor is running
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(start.FileName)).Any())
            {
                return;
            }

            // Start in a new thread to dec
            Process.Start(Path.GetFullPath(@"..\..\..\..\WorkerRoleCommandProcessor\bin\Debug\CommandProcessor.exe"));
            // Wait for processor initialization and warm up
            Thread.Sleep(Constants.CommandProcessorWaitTimeout);
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

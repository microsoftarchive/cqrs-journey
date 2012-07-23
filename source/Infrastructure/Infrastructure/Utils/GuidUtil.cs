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

namespace Infrastructure.Utils
{
    using System;

    public static class GuidUtil
    {
        private static readonly long EpochMilliseconds = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000L;

        /// <summary>
        /// Creates a sequential GUID according to SQL Server's ordering rules.
        /// </summary>
        public static Guid NewSequentialId()
        {
            // This code was not reviewed to guarantee uniqueness under most conditions, nor completely optimize for avoiding
            // page splits in SQL Server when doing inserts from multiple hosts, so do not re-use in production systems.
            var guidBytes = Guid.NewGuid().ToByteArray();

            // get the milliseconds since Jan 1 1970
            byte[] sequential = BitConverter.GetBytes((DateTime.Now.Ticks / 10000L) - EpochMilliseconds);

            // discard the 2 most significant bytes, as we only care about the milliseconds increasing, but the highest ones 
            // should be 0 for several thousand years to come (non-issue).
            if (BitConverter.IsLittleEndian)
            {
                guidBytes[10] = sequential[5];
                guidBytes[11] = sequential[4];
                guidBytes[12] = sequential[3];
                guidBytes[13] = sequential[2];
                guidBytes[14] = sequential[1];
                guidBytes[15] = sequential[0];
            }
            else
            {
                Buffer.BlockCopy(sequential, 2, guidBytes, 10, 6);
            }

            return new Guid(guidBytes);
        }
    }
}

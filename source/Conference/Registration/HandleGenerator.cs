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
using System.Linq;
using System.Text;

namespace Registration
{
    /// <summary>
    /// Generates random alphnumerical strings.
    /// </summary>
    public static class HandleGenerator
    {
        private static Random rnd = new Random(DateTime.UtcNow.Millisecond);
        private static char[] allowableChars = "ABCDEFGHJKMNPQRSTUVWXYZ123456789".ToCharArray();

        public static string Generate(int length)
        {
            var result = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                result.Append(allowableChars[rnd.Next(0, allowableChars.Length)]);
            }

            return result.ToString();
        }
    }
}

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

namespace Infrastructure.Azure
{
    using System;

    public static class DateTimeExtensions
    {
        private static readonly DateTime EpochBaseline = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToEpochMilliseconds(this DateTime date)
        {
            var difference = date.ToUniversalTime() - EpochBaseline;
            return Convert.ToInt64(difference.TotalMilliseconds);
        }

        public static DateTime ToDateTime(this long millisecondsSince1970)
        {
            return EpochBaseline.AddMilliseconds(millisecondsSince1970);
        }
    }
}

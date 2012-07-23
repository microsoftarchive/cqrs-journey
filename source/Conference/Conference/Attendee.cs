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

using System.ComponentModel.DataAnnotations;

namespace Conference
{
    /// <summary>
    /// Represents an attendee to the conference, someone who has been 
    /// assigned to a purchased seat.
    /// </summary>
    [ComplexType]
    public class Attendee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // NOTE: we validate incoming data (this is filled from an event coming 
        // from the registration BC) so that when EF saves it will fail if it's invalid.
        [RegularExpression(@"[\w-]+(\.?[\w-])*\@[\w-]+(\.[\w-]+)+")]
        public string Email { get; set; }
    }
}

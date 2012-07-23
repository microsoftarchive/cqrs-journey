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

namespace Registration
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Registration.Properties;

    public class PersonalInfo : IEquatable<PersonalInfo>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [RegularExpression(@"[\w-]+(\.?[\w-])*\@[\w-]+(\.[\w-]+)+", ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "InvalidEmail")]
        public string Email { get; set; }

        #region Equality

        public static bool operator ==(PersonalInfo obj1, PersonalInfo obj2)
        {
            return PersonalInfo.Equals(obj1, obj2);
        }

        public static bool operator !=(PersonalInfo obj1, PersonalInfo obj2)
        {
            return !(obj1 == obj2);
        }

        public bool Equals(PersonalInfo other)
        {
            return PersonalInfo.Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return PersonalInfo.Equals(this, obj as PersonalInfo);
        }

        public static bool Equals(PersonalInfo obj1, PersonalInfo obj2)
        {
            if (Object.Equals(obj1, null) && Object.Equals(obj2, null)) return true;
            if (Object.ReferenceEquals(obj1, obj2)) return true;

            if (Object.Equals(null, obj1) ||
                Object.Equals(null, obj2) ||
                obj1.GetType() != obj2.GetType())
                return false;

            // Compare your object properties
            return string.Equals(obj1.Email, obj2.Email, StringComparison.InvariantCultureIgnoreCase) &&
                obj1.FirstName == obj2.FirstName &&
                obj1.LastName == obj2.LastName;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (this.Email != null)
                hash ^= this.Email.GetHashCode();
            if (this.FirstName != null)
                hash ^= this.FirstName.GetHashCode();
            if (this.LastName != null)
                hash ^= this.LastName.GetHashCode();

            return hash;
        }

        #endregion
    }
}

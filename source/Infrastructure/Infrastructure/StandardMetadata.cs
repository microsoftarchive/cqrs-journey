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

namespace Infrastructure
{
    /// <summary>
    /// Exposes the property names of standard metadata added to all 
    /// messages going through the bus.
    /// </summary>
    public static class StandardMetadata
    {
        /// <summary>
        /// Identifier of the object that originated the event, if any.
        /// </summary>
        public const string SourceId = "SourceId";

        /// <summary>
        /// The simple assembly name of the message payload (i.e. event or command).
        /// </summary>
        public const string AssemblyName = "AssemblyName";

        /// <summary>
        /// The namespace of the message payload (i.e. event or command).
        /// </summary>
        public const string Namespace = "Namespace";

        /// <summary>
        /// The full type name of the message payload (i.e. event or command).
        /// </summary>
        public const string FullName = "FullName";

        /// <summary>
        /// The simple type name (without the namespace) of the message payload (i.e. event or command).
        /// </summary>
        public const string TypeName = "TypeName";
    }
}

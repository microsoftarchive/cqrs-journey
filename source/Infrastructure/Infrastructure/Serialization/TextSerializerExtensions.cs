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

namespace Infrastructure.Serialization
{
    using System.IO;

    /// <summary>
    /// Usability overloads for <see cref="ITextSerializer"/>.
    /// </summary>
    public static class TextSerializerExtensions
    {
        /// <summary>
        /// Serializes the given data object as a string.
        /// </summary>
        public static string Serialize<T>(this ITextSerializer serializer, T data)
        {
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, data);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserializes the specified string into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="System.InvalidCastException">The deserialized object is not of type <typeparamref name="T"/>.</exception>
        public static T Deserialize<T>(this ITextSerializer serializer, string serialized)
        {
            using (var reader = new StringReader(serialized))
            {
                return (T)serializer.Deserialize(reader);
            }
        }
    }
}

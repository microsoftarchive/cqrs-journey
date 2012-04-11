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

namespace Common
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    /// Serializes using a binary formatter.
    /// </summary>
    public class BinarySerializer : ISerializer
    {
        /// <summary>
        /// Serializes an object graph to a stream.
        /// </summary>
        public void Serialize(Stream stream, object graph)
        {
            new BinaryFormatter().Serialize(stream, graph);
        }

        /// <summary>
        /// Deserializes an object graph of the given <paramref name="objectType"/>
        /// from the specified stream.
        /// </summary>
        public object Deserialize(Stream stream)
        {
            return new BinaryFormatter().Deserialize(stream);
        }
    }
}

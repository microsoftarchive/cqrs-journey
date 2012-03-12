// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Common
{
    using System;
    using System.IO;

    /// <summary>
    /// Interface for serializers that can read/write an object graph to a stream.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes an object graph to a stream.
        /// </summary>
        void Serialize(Stream stream, object graph);

        /// <summary>
        /// Deserializes an object graph of the given <paramref name="objectType"/> 
        /// from the specified stream.
        /// </summary>
        object Deserialize(Stream stream, Type objectType);
    }
}

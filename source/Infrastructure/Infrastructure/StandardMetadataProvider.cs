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

namespace Infrastructure
{
    using System.Collections.Generic;
    using System.IO;
    using Infrastructure.Messaging;

    /// <summary>
    /// Extracts metadata about a payload so that it's placed in the 
    /// message envelope.
    /// </summary>
    public class StandardMetadataProvider : IMetadataProvider
    {
        /// <summary>
        /// Gets metadata associated with the payload, which can be
        /// used by processors to filter and selectively subscribe to
        /// messages.
        /// </summary>
        public virtual IDictionary<string, string> GetMetadata(object payload)
        {
            var metadata = new Dictionary<string, string>();
            var type = payload.GetType();

            // The standard metadata could be used as a sort of partitioning already, 
            // maybe considering different assembly names as being the area/subsystem/bc.

            metadata[StandardMetadata.AssemblyName] = Path.GetFileNameWithoutExtension(type.Assembly.ManifestModule.FullyQualifiedName);
            metadata[StandardMetadata.FullName] = type.FullName;
            metadata[StandardMetadata.Namespace] = type.Namespace;
            metadata[StandardMetadata.TypeName] = type.Name;

            var e = payload as IEvent;
            if (e != null)
            {
                metadata[StandardMetadata.SourceId] = e.SourceId.ToString();
                metadata[StandardMetadata.Kind] = StandardMetadata.EventKind;
            }

            var c = payload as ICommand;
            if (c != null)
            {
                metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
            }

            // NOTE: here we may add an "Area" or "Subsystem" or 
            // whatever via .NET custom attributes on the payload 
            // type.

            return metadata;
        }
    }
}

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

namespace Infrastructure.MessageLog
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The query criteria for filtering events from the message log when reading.
    /// </summary>
    public class QueryCriteria
    {
        public QueryCriteria()
        {
            this.SourceTypes = new List<string>();
            this.SourceIds = new List<string>();
            this.AssemblyNames = new List<string>();
            this.Namespaces = new List<string>();
            this.FullNames = new List<string>();
            this.TypeNames = new List<string>();
        }

        public ICollection<string> SourceTypes { get; private set; }
        public ICollection<string> SourceIds { get; private set; }
        public ICollection<string> AssemblyNames { get; private set; }
        public ICollection<string> Namespaces { get; private set; }
        public ICollection<string> FullNames { get; private set; }
        public ICollection<string> TypeNames { get; private set; }
        public DateTime? EndDate { get; set; }
    }
}

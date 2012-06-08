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

// Based on http://blogs.msdn.com/b/pfxteam/archive/2010/04/06/9990420.aspx

namespace Infrastructure.Azure.Utils
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    internal class BlockingCollectionPartitioner<T> : Partitioner<T>
    {
        private readonly BlockingCollection<T> collection;

        internal BlockingCollectionPartitioner(BlockingCollection<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            this.collection = collection;
        }

        public override bool SupportsDynamicPartitions
        {
            get { return true; }
        }

        public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
        {
            if (partitionCount < 1) throw new ArgumentOutOfRangeException("partitionCount");

            var dynamicPartitioner = GetDynamicPartitions();
            return Enumerable.Range(0, partitionCount).Select(_ => dynamicPartitioner.GetEnumerator()).ToArray();
        }

        public override IEnumerable<T> GetDynamicPartitions()
        {
            return collection.GetConsumingEnumerable();
        }
    }
}

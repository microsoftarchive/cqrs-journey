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

namespace Infrastructure.Util
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Prevents double enumeration (and potential roundtrip to the data source) when checking 
    /// for the presence of items in an enumeration.
    /// </summary>
    internal static class CacheAnyEnumerableExtensions
    {
        /// <summary>
        /// Makes sure that calls to <see cref="IAnyEnumerable{T}.Any()"/> are 
        /// cached, and reuses the resulting enumerator.
        /// </summary>
        public static IAnyEnumerable<T> AsCachedAnyEnumerable<T>(this IEnumerable<T> source)
        {
            return new AnyEnumerable<T>(source);
        }

        /// <summary>
        /// Exposes a cached <see cref="Any"/> operator.
        /// </summary>
        public interface IAnyEnumerable<out T> : IEnumerable<T>
        {
            bool Any();
        }

        /// <summary>
        /// Lazily computes whether the inner enumerable has 
        /// any values, and caches the result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "By design")]
        private class AnyEnumerable<T> : IAnyEnumerable<T>
        {
            private readonly IEnumerable<T> enumerable;
            private IEnumerator<T> enumerator;
            private bool hasAny;

            public AnyEnumerable(IEnumerable<T> enumerable)
            {
                this.enumerable = enumerable;
            }

            public bool Any()
            {
                this.InitializeEnumerator();

                return this.hasAny;
            }

            public IEnumerator<T> GetEnumerator()
            {
                this.InitializeEnumerator();

                return this.enumerator;
            }

            private void InitializeEnumerator()
            {
                if (this.enumerator == null)
                {
                    var inner = this.enumerable.GetEnumerator();
                    this.hasAny = inner.MoveNext();
                    this.enumerator = new SkipFirstEnumerator(inner, this.hasAny);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            private class SkipFirstEnumerator : IEnumerator<T>
            {
                private readonly IEnumerator<T> inner;
                private readonly bool hasNext;
                private bool isFirst = true;

                public SkipFirstEnumerator(IEnumerator<T> inner, bool hasNext)
                {
                    this.inner = inner;
                    this.hasNext = hasNext;
                }

                public T Current { get { return this.inner.Current; } }

                public void Dispose()
                {
                    this.inner.Dispose();
                }

                object IEnumerator.Current { get { return this.Current; } }

                public bool MoveNext()
                {
                    if (this.isFirst)
                    {
                        this.isFirst = false;
                        return this.hasNext;
                    }

                    return this.inner.MoveNext();
                }

                public void Reset()
                {
                    this.inner.Reset();
                }
            }
        }
    }
}

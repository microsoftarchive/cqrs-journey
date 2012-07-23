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

namespace System.Collections.Generic
{
    /// <summary>
    /// Usability extensions for dictionaries.
    /// </summary>
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Gets an item from the dictionary, if it's found.
        /// </summary>
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, default(TValue));
        }

        /// <summary>
        /// Gets an item from the dictionary, if it's found. Otherwise, 
        /// returns the specified default value.
        /// </summary>
        public static TValue TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            var result = defaultValue;
            if (!dictionary.TryGetValue(key, out result))
                return defaultValue;

            return result;
        }
    }
}

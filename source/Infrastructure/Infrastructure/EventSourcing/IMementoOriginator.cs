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

namespace Infrastructure.EventSourcing
{
    /// <summary>
    /// Defines that the implementor can create memento objects (snapshots), that can be used to recreate the original state.
    /// </summary>
    public interface IMementoOriginator
    {
        /// <summary>
        /// Saves the object's state to an opaque memento object (a snapshot) that can be used to restore the state.
        /// </summary>
        /// <returns>An opaque memento object that can be used to restore the state.</returns>
        IMemento SaveToMemento();
    }

    /// <summary>
    /// An opaque object that contains the state of another object (a snapshot) and can be used to restore its state.
    /// </summary>
    public interface IMemento
    {
        /// <summary>
        /// The version of the <see cref="IEventSourced"/> instance.
        /// </summary>
        int Version { get; }
    }
}

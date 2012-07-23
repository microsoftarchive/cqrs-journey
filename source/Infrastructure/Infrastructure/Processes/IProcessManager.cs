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

namespace Infrastructure.Processes
{
    using System;
    using System.Collections.Generic;
    using Infrastructure.Messaging;

    /// <summary>
    /// Interface implemented by process managers (also known as Sagas in the CQRS community) that 
    /// publish commands to the command bus.
    /// </summary>
    /// <remarks>
    /// <para>See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258564">Reference 6</see> for a description of what is a Process Manager.</para>
    /// <para>There are a few things that we learnt along the way regarding Process Managers, which we might do differently with the new insights that we
    /// now have. See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258558"> Journey lessons learnt</see> for more information.</para>
    /// </remarks>
    public interface IProcessManager
    {
        /// <summary>
        /// Gets the process manager identifier.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets a value indicating whether the process manager workflow is completed and the state can be archived.
        /// </summary>
        bool Completed { get; }

        /// <summary>
        /// Gets a collection of commands that need to be sent when the state of the process manager is persisted.
        /// </summary>
        IEnumerable<Envelope<ICommand>> Commands { get; }
    }
}

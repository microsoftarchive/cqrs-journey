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

namespace Infrastructure.Azure.Messaging
{
    using System;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Provides the brokered message payload of an event.
    /// </summary>
    public class BrokeredMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BrokeredMessageEventArgs"/> class.
        /// </summary>
        public BrokeredMessageEventArgs(BrokeredMessage message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Gets the message associated with the event.
        /// </summary>
        public BrokeredMessage Message { get; private set; }

        /// <summary>
        /// Gets or sets an indication that the message should not be disposed by the originating receiver.
        /// </summary>
        public bool DoNotDisposeMessage { get; set; }
    }
}

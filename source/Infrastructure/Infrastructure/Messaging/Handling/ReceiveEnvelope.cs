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

namespace Infrastructure.Messaging.Handling
{
    using System;

    /// <summary>
    /// Static factory class for <see cref="ReceiveEnvelope{T}"/>.
    /// </summary>
    public abstract class ReceiveEnvelope
    {
        protected ReceiveEnvelope(string messageId, string correlationId)
        {
            this.MessageId = messageId;
            this.CorrelationId = correlationId;
        }

        /// <summary>
        /// Creates an envelope for the given body.
        /// </summary>
        public static ReceiveEnvelope<T> Create<T>(T body, string messageId, string correlationId)
        {
            return new ReceiveEnvelope<T>(body, messageId, correlationId);
        }

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        public string CorrelationId { get; private set; }

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        public string MessageId { get; private set; }
    }

    /// <summary>
    /// Provides the envelope for an object that will be sent to a bus.
    /// </summary>
    public class ReceiveEnvelope<T> : ReceiveEnvelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveEnvelope{T}"/> class.
        /// </summary>
        public ReceiveEnvelope(T body, string messageId, string correlationId)
            : base(messageId, correlationId)
        {
            this.Body = body;
        }

        /// <summary>
        /// Gets the body.
        /// </summary>
        public T Body { get; private set; }
    }
}

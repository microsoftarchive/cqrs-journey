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

namespace Infrastructure.Messaging
{
    using System;

    /// <summary>
    /// Static factory class for <see cref="Envelope{T}"/>.
    /// </summary>
    public abstract class Envelope
    {
        /// <summary>
        /// Creates an envelope for the given body.
        /// </summary>
        public static Envelope<T> Create<T>(T body)
        {
            return new Envelope<T>(body);
        }
    }

    /// <summary>
    /// Provides the envelope for an object that will be sent to a bus.
    /// </summary>
    public class Envelope<T> : Envelope
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope{T}"/> class.
        /// </summary>
        public Envelope(T body)
        {
            this.Body = body;
        }

        /// <summary>
        /// Gets the body.
        /// </summary>
        public T Body { get; private set; }

        /// <summary>
        /// Gets or sets the delay for sending, enqueing or processing the body.
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// Gets or sets the time to live for the message in the queue.
        /// </summary>
        public TimeSpan TimeToLive { get; set; }

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        public string MessageId { get; set; }

        public static implicit operator Envelope<T>(T body)
        {
            return Envelope.Create(body);
        }
    }
}

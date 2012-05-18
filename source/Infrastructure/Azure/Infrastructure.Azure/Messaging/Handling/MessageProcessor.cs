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

namespace Infrastructure.Azure.Messaging.Handling
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Utils;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Provides basic common processing code for components that handle 
    /// incoming messages from a receiver.
    /// </summary>
    public abstract class MessageProcessor : IProcessor, IDisposable
    {
        private const int MaxProcessingRetries = 5;
        private bool disposed;
        private bool started = false;
        private readonly IMessageReceiver receiver;
        private readonly ITextSerializer serializer;
        private readonly object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessor"/> class.
        /// </summary>
        protected MessageProcessor(IMessageReceiver receiver, ITextSerializer serializer)
        {
            this.receiver = receiver;
            this.serializer = serializer;
        }

        protected ITextSerializer Serializer { get { return this.serializer; } }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public virtual void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject)
            {
                if (!this.started)
                {
                    this.receiver.MessageReceived += OnMessageReceived;
                    this.receiver.Start();
                    this.started = true;
                }
            }
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public virtual void Stop()
        {
            lock (this.lockObject)
            {
                if (this.started)
                {
                    this.receiver.Stop();
                    this.receiver.MessageReceived -= OnMessageReceived;
                    this.started = false;
                }
            }
        }

        /// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void ProcessMessage(object payload);

        /// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Stop();
                    this.disposed = true;

                    using (this.receiver as IDisposable)
                    {
                        // Dispose receiver if it's disposable.
                    }
                }
            }
        }

        ~MessageProcessor()
        {
            Dispose(false);
        }

        private void OnMessageReceived(object sender, BrokeredMessageEventArgs args)
        {
            // NOTE: type information does not belong here. It's a responsibility 
            // of the serializer to be self-contained and put any information it 
            // might need for rehydration.
            var message = args.Message;

            object payload;
            using (var stream = message.GetBody<Stream>())
            using (var reader = new StreamReader(stream))
            {
                payload = this.serializer.Deserialize(reader);
            }

            // TODO: have a better trace correlation mechanism (that is used in both the sender and receiver).
            string traceIdentifier = BuildTraceIdentifier(message);
            try
            {
                TracePayload(traceIdentifier, payload);

                ProcessMessage(payload);
            }
            catch (Exception e)
            {
                if (args.Message.DeliveryCount > MaxProcessingRetries)
                {
                    Trace.TraceWarning("An error occurred while processing the message" + traceIdentifier + " and will be dead-lettered:\r\n{0}", e);
                    message.SafeDeadLetter(e.Message, e.ToString());
                }
                else
                {
                    Trace.TraceWarning("An error occurred while processing the message" + traceIdentifier + " and will be abandoned:\r\n{0}", e);
                    message.SafeAbandon();
                }

                return;
            }

            Trace.WriteLine("The message" + traceIdentifier + " has been processed and will be completed.");
            message.SafeComplete();
        }

        // TODO: remove once we have a better trace correlation mechanism (that is used in both the sender and receiver).
        private static string BuildTraceIdentifier(BrokeredMessage message)
        {
            try
            {
                var messageId = message.MessageId;
                if (!string.IsNullOrEmpty(messageId))
                {
                    return string.Format(CultureInfo.InvariantCulture, " (MessageId: {0})", messageId);
                }

                var sourceId = message.Properties.TryGetValue(StandardMetadata.SourceId) as string;
                if (!string.IsNullOrEmpty(sourceId))
                {
                    return string.Format(CultureInfo.InvariantCulture, " (with SourceId: {0})", sourceId);
                }
            }
            catch (ObjectDisposedException)
            {
                // if there is any kind of exception trying to build a trace identifier, ignore, as it is not important.
            }

            return string.Empty;
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
                throw new ObjectDisposedException("MessageProcessor");
        }

        [Conditional("TRACE")]
        private void TracePayload(string traceIdentifier, object payload)
        {
            // TODO: can force the use of indented JSON for trace
            using (var writer = new StringWriter())
            {
                this.Serializer.Serialize(writer, payload);
                Trace.WriteLine("Processing message" + traceIdentifier + " with payload:\r\n" + writer.ToString());
            }
        }
    }
}

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

namespace Azure
{
    using System;
    using System.IO;
    using Azure.Messaging;
    using Common;

    /// <summary>
    /// Provides basic common processing code for components that handle 
    /// incoming messages from a receiver.
    /// </summary>
    public abstract class MessageProcessor : IDisposable
    {
        private bool disposed;
        private bool started = false;
        private IMessageReceiver receiver;
        private ISerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessor"/> class.
        /// </summary>
        protected MessageProcessor(IMessageReceiver receiver, ISerializer serializer)
        {
            this.receiver = receiver;
            this.serializer = serializer;
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public virtual void Start()
        {
            ThrowIfDisposed();
            if (this.started)
                throw new InvalidOperationException("Already started");

            this.receiver.MessageReceived += OnMessageReceived;
            this.receiver.Start();
            this.started = true;
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public virtual void Stop()
        {
            ThrowIfDisposed();
            if (!this.started)
                throw new InvalidOperationException("Not started");

            this.receiver.Stop();
            this.receiver.MessageReceived -= OnMessageReceived;
            this.started = false;
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
                    if (this.started)
                        this.Stop();

                    var disposable = this.receiver as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }

                this.disposed = true;
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
            // might need for rehytration.
            var message = args.Message;

            using (var stream = message.GetBody<Stream>())
            {
                var payload = this.serializer.Deserialize(stream);
                // TODO: error handling if handlers fail?
                try
                {
                    ProcessMessage(payload);
                    message.Async(message.BeginComplete, message.EndComplete);
                }
                catch (Exception)
                {
                    // TODO: retries, retry count, Abandon vs DeadLetter?
                    args.Message.Async(args.Message.BeginDeadLetter, args.Message.EndDeadLetter);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
                throw new ObjectDisposedException("MessageProcessor");
        }
    }
}

// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
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
    using System.Reflection;
    using Azure.Messaging;
    using Common;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Provides basic common processing code for components that handle 
    /// incoming messages from a receiver.
    /// </summary>
    public abstract class MessageProcessor : IListener, IDisposable
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

        protected abstract void ProcessMessage(object payload, Type payloadType);

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
            // Grab type information from message properties.

            object typeValue = null;
            object assemblyValue = null;

            if (args.Message.Properties.TryGetValue("Type", out typeValue) &&
                args.Message.Properties.TryGetValue("Assembly", out assemblyValue))
            {
                var typeName = (string)args.Message.Properties["Type"];
                var assemblyName = (string)args.Message.Properties["Assembly"];

                var type = Type.GetType(typeName);

                if (type != null)
                {
                    this.ProcessMessage(args.Message, type);
                    return;
                }

                var assembly = Assembly.LoadWithPartialName(assemblyName);
                if (assembly != null)
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        this.ProcessMessage(args.Message, type);
                        return;
                    }
                }
            }

            // TODO: if we got here, it's 'cause we couldn't read the type.
            // Should we throw? Log? Ignore?
            args.Message.Async(args.Message.BeginAbandon, args.Message.EndAbandon);
        }

        private void ProcessMessage(BrokeredMessage message, Type messageType)
        {
            using (var stream = message.GetBody<Stream>())
            {
                var payload = this.serializer.Deserialize(stream, messageType);
                // TODO: error handling if handlers fail?
                ProcessMessage(payload, messageType);
            }

            message.Async(message.BeginComplete, message.EndComplete);
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
                throw new ObjectDisposedException("MessageProcessor");
        }
    }
}

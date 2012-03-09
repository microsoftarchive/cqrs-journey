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
    using System.Runtime.Serialization.Json;
    using System.Threading;
    using Common;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class MessageReceiver
    {
        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly MessageBusSettings settings;

        public MessageReceiver(MessageBusSettings settings, CancellationToken cancellationToken)
        {
            this.settings = settings;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);
        }

        public void Send<T>(Envelope<T> message)
        {
            var serializer = new DataContractJsonSerializer(message.GetType());
            var factory = MessagingFactory.Create(this.serviceUri, this.tokenProvider);
            var commandSender = factory.CreateMessageSender(this.settings.Topic);

            var brokeredMessage = new BrokeredMessage(Serialize(message));
            brokeredMessage.Properties["Type"] = message.GetType().FullName;
            commandSender.Send(brokeredMessage);
        }

        private static string Serialize(object payload)
        {
            var serializer = new DataContractJsonSerializer(payload.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, payload);

                return Convert.ToBase64String(stream.ToArray());
            }
        }
    }
}

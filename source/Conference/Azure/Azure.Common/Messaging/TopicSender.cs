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

namespace Azure.Messaging
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Common;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class TopicSender
    {
        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly BusSettings settings;

        public TopicSender(BusSettings settings)
        {
            this.settings = settings;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            try
            {
                new NamespaceManager(this.serviceUri, this.tokenProvider)
                    .CreateTopic(
                        new TopicDescription(settings.Topic)
                        {
                            RequiresDuplicateDetection = true,
                            DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(30)
                        });
            }
            catch (MessagingEntityAlreadyExistsException)
            { }
        }

        /// <summary>
        /// Asynchronously sends the specified message.
        /// </summary>
        public void Send<T>(Envelope<T> message)
        {
            var factory = MessagingFactory.Create(this.serviceUri, this.tokenProvider);
            var client = factory.CreateTopicClient(this.settings.Topic);

            var brokeredMessage = new BrokeredMessage(Serialize(message.Body));
            brokeredMessage.Properties["Type"] = message.GetType().FullName;
            brokeredMessage.Properties["Assembly"] = Path.GetFileNameWithoutExtension(message.GetType().Assembly.ManifestModule.FullyQualifiedName);

            // Always send async.
            client.BeginSend(brokeredMessage, new AsyncCallback(this.OnSendCompleted), client);
        }

        private void OnSendCompleted(IAsyncResult result)
        {
            var client = (TopicClient)result.AsyncState;

            client.EndSend(result);
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

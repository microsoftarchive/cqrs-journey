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
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class TopicReceiver
    {
        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly BusSettings settings;
        private CancellationTokenSource cancellationSource;
        private MessageReceiver messageReceiver;

        public TopicReceiver(BusSettings settings)
        {
            this.settings = settings;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            var messagingFactory = MessagingFactory.Create(this.serviceUri, tokenProvider);
            this.messageReceiver = messagingFactory.CreateMessageReceiver(@"Commands/subscriptions/All");
        }

        public void Start()
        {
            if (this.cancellationSource != null)
                throw new InvalidOperationException();

            this.cancellationSource = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        var message = this.messageReceiver.Receive();

                        if (message == null)
                        {
                            Thread.Sleep(500);
                            continue;
                        }

                        try
                        {
                            var type = (string)message.Properties["Type"];
                            var payloadString = message.GetBody<string>();

                            // var payload = (T)Deserialize(payloadString);

                            message.Complete();
                        }
                        catch (Exception e)
                        {
                            message.Abandon();
                            throw e;
                        }
                    }
                },
                this.cancellationSource.Token);
        }

        public void Stop()
        {
            if (this.cancellationSource == null)
                throw new InvalidOperationException();

            this.cancellationSource.Cancel();
            this.cancellationSource = null;
        }
    }
}

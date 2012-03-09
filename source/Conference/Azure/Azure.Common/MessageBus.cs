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

namespace Azure.Common
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Microsoft.ServiceBus;
	using System.Runtime.Serialization.Json;
	using Microsoft.ServiceBus.Messaging;

	public class MessageBus
	{
		private readonly TokenProvider tokenProvider;
		private readonly Uri serviceUri;
		private readonly MessageBusSettings settings;

		public MessageBus(MessageBusSettings settings)
		{
			this.settings = settings;

			this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
			this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);
		}

		public void Send<T>(T body)
		{
			var serializer = new DataContractJsonSerializer(body.GetType());
			// new BrokeredMessage().
			// TODO: serialize, send, etc.
		}
	}
}

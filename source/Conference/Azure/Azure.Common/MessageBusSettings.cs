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
    public class MessageBusSettings
    {
        public MessageBusSettings()
        {
            this.Topic = string.Empty;

            this.ServiceUriScheme = string.Empty;
            this.ServiceNamespace = string.Empty;
            this.ServicePath = string.Empty;

            this.TokenIssuer = string.Empty;
            this.TokenAccessKey = string.Empty;
        }

        public string ServiceUriScheme { get; set; }
        public string ServiceNamespace { get; set; }
        public string ServicePath { get; set; }

        public string TokenIssuer { get; set; }
        public string TokenAccessKey { get; set; }

        public string Topic { get; set; }
    }
}

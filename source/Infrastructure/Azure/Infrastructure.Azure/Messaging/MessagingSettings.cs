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

namespace Infrastructure.Azure.Messaging
{
    using System.Xml.Serialization;

    /// <summary>
    /// Simple settings class to configure the connection to Azure 
    /// messaging APIs.
    /// </summary>
    [XmlRoot("Messaging")]
    public class MessagingSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingSettings"/> class.
        /// </summary>
        public MessagingSettings()
        {
            this.ServiceUriScheme = string.Empty;
            this.ServiceNamespace = string.Empty;
            this.ServicePath = string.Empty;

            this.TokenIssuer = string.Empty;
            this.TokenAccessKey = string.Empty;
        }

        /// <summary>
        /// Gets or sets the service URI scheme.
        /// </summary>
        public string ServiceUriScheme { get; set; }
        /// <summary>
        /// Gets or sets the service namespace.
        /// </summary>
        public string ServiceNamespace { get; set; }
        /// <summary>
        /// Gets or sets the service path.
        /// </summary>
        public string ServicePath { get; set; }

        /// <summary>
        /// Gets or sets the token issuer.
        /// </summary>
        public string TokenIssuer { get; set; }
        /// <summary>
        /// Gets or sets the token access key.
        /// </summary>
        public string TokenAccessKey { get; set; }
    }
}

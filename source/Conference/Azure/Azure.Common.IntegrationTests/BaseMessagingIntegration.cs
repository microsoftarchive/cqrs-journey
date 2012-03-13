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

namespace Azure.IntegrationTests
{
    using System;
    using System.IO;
    using Azure.Messaging;
    using Xunit;

    /// <summary>
    /// Base class for messaging integration tests.
    /// </summary>
    public class given_messaging_settings
    {
        public given_messaging_settings()
        {
            var baseDir = @"..\..\..\ConsoleCommandProcessor";
            var templateFile = new FileInfo(Path.Combine(baseDir, "Settings.Template.xml")).FullName;
            var settingsFile = new FileInfo(Path.Combine(baseDir, "Settings.xml")).FullName;
            Assert.True(File.Exists("Settings.xml"), string.Format(
                "Please rename {0} to {1} and replace the placeholders with your Azure credentials. Then rebuild and re-run the tests.",
                templateFile, settingsFile));

            this.Settings = MessagingSettings.Read("Settings.xml");
        }

        public MessagingSettings Settings { get; private set; }
    }

    public class given_a_topic_and_subscription : given_messaging_settings
    {
        public given_a_topic_and_subscription()
        {
            this.Topic = Guid.NewGuid().ToString();
            this.Subscription = Guid.NewGuid().ToString();

            // Creates the topic too.
            this.Settings.CreateSubscription(this.Topic, this.Subscription);
        }

        public virtual void Dispose()
        {
            // Deletes subscriptions too.
            this.Settings.TryDeleteTopic(this.Topic);
        }

        public string Topic { get; private set; }
        public string Subscription { get; private set; }
    }
}

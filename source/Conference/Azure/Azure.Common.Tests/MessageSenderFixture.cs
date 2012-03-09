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

namespace Azure.Tests
{
    using System;
    using Common;
    using Xunit;
    using Azure.Messaging;

    public class GivenAMessageSender
    {
        [Fact]
        public void WhenSendingMessage_ThenSucceeds()
        {


            var sender = new Sender(new BusSettings
            {
                ServiceNamespace = "danielkzu",
                ServiceUriScheme = "sb",
                TokenIssuer = "owner",
                TokenAccessKey = "4q2LqEP9HTlLqGvyhiGxZ0DJGjYEUPfC/zstbSeopuI=",
                Topic = "Commands",
            });

            sender.Send(Envelope.Create(new Command { Id = Guid.NewGuid(), Title = "DoSomething" }));
        }

        public class Command
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
        }
    }
}

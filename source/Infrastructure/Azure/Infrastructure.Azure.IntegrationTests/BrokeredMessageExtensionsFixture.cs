// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Azure.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using Infrastructure.Azure.Utils;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Xunit;

    public class BrokeredMessageExtensionsFixture
    {
        [Fact]
        public void when_failing_transiently_then_retries()
        {
            int endCounts = 0;
            bool? success = null;
            var stopwatch = Stopwatch.StartNew();

            BrokeredMessageExtensions
                .SafeMessagingActionAsync(
                   c => c(Mock.Of<IAsyncResult>()),
                   ar => { if (++endCounts < 2) throw new TimeoutException(); },
                   new BrokeredMessage(),
                   s => success = s,
                   "error: '{0}' '{1}' '{2}' '{3}' '{4}' '{5}' '{6}'",
                   "message id",
                   "sub",
                   5000,
                   1000,
                   stopwatch);

            Assert.Equal(2, endCounts);
            Assert.True(success.HasValue);
            Assert.True(success.Value);
            Assert.False(stopwatch.IsRunning);
        }

        [Fact]
        public void when_failing_transiently_then_retries_until_maximum_retries()
        {
            bool? success = null;
            var stopwatch = Stopwatch.StartNew();

            BrokeredMessageExtensions
                .SafeMessagingActionAsync(
                   c => c(Mock.Of<IAsyncResult>()),
                   ar => { throw new TimeoutException(); },
                   new BrokeredMessage(),
                   s => success = s,
                   "error: '{0}' '{1}' '{2}' '{3}' '{4}' '{5}' '{6}'",
                   "message id",
                   "sub",
                   5000,
                   100,
                   stopwatch);

            Assert.True(success.HasValue);
            Assert.False(success.Value);
            Assert.False(stopwatch.IsRunning);
        }
    }
}

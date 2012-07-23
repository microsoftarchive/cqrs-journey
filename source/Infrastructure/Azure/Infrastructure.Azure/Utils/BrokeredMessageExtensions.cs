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

namespace Infrastructure.Azure.Utils
{
    using System;
    using System.Diagnostics;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus.Messaging;

    public static class BrokeredMessageExtensions
    {
        private static readonly RetryStrategy retryStrategy =
            new ExponentialBackoff(3, TimeSpan.FromSeconds(.5d), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2)) { FastFirstRetry = true };

        public static void SafeCompleteAsync(this BrokeredMessage message, string subscription, Action<bool> callback, long processingElapsedMilliseconds, long schedulingElapsedMilliseconds, Stopwatch roundtripStopwatch)
        {
            SafeMessagingActionAsync(
                ac => message.BeginComplete(ac, null),
                message.EndComplete,
                message,
                callback,
                "An error occurred while completing message {0} in subscription {1} with processing time {3} (scheduling {4} request {5} roundtrip {6}). Error message: {2}",
                message.MessageId,
                subscription,
                processingElapsedMilliseconds,
                schedulingElapsedMilliseconds,
                roundtripStopwatch);
        }

        public static void SafeAbandonAsync(this BrokeredMessage message, string subscription, Action<bool> callback, long processingElapsedMilliseconds, long schedulingElapsedMilliseconds, Stopwatch roundtripStopwatch)
        {
            SafeMessagingActionAsync(
                ac => message.BeginAbandon(ac, null),
                message.EndAbandon,
                message,
                callback,
                "An error occurred while abandoning message {0} in subscription {1} with processing time {3} (scheduling {4} request {5} roundtrip {6}). Error message: {2}",
                message.MessageId,
                subscription,
                processingElapsedMilliseconds,
                schedulingElapsedMilliseconds,
                roundtripStopwatch);
        }

        public static void SafeDeadLetterAsync(this BrokeredMessage message, string subscription, string reason, string description, Action<bool> callback, long processingElapsedMilliseconds, long schedulingElapsedMilliseconds, Stopwatch roundtripStopwatch)
        {
            SafeMessagingActionAsync(
                ac => message.BeginDeadLetter(reason, description, ac, null),
                message.EndDeadLetter,
                message,
                callback,
                "An error occurred while dead-lettering message {0} in subscription {1} with processing time {3} (scheduling {4} request {5} roundtrip {6}). Error message: {2}",
                message.MessageId,
                subscription,
                processingElapsedMilliseconds,
                schedulingElapsedMilliseconds,
                roundtripStopwatch);
        }

        internal static void SafeMessagingActionAsync(Action<AsyncCallback> begin, Action<IAsyncResult> end, BrokeredMessage message, Action<bool> callback, string actionErrorDescription, string messageId, string subscription, long processingElapsedMilliseconds, long schedulingElapsedMilliseconds, Stopwatch roundtripStopwatch)
        {
            var retryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(retryStrategy);
            retryPolicy.Retrying +=
                (s, e) =>
                {
                    Trace.TraceWarning("An error occurred in attempt number {1} to release message {3} in subscription {2}: {0}",
                    e.LastException.GetType().Name + " - " + e.LastException.Message,
                    e.CurrentRetryCount,
                    subscription,
                    message.MessageId);
                };

            long messagingActionStart = 0;

            retryPolicy.ExecuteAction(
                ac => { messagingActionStart = roundtripStopwatch.ElapsedMilliseconds; begin(ac); },
                end,
                () =>
                {
                    roundtripStopwatch.Stop();
                    callback(true);
                },
                e =>
                {
                    roundtripStopwatch.Stop();

                    if (e is MessageLockLostException || e is MessagingException || e is TimeoutException)
                    {
                        Trace.TraceWarning(actionErrorDescription, messageId, subscription, e.GetType().Name + " - " + e.Message, processingElapsedMilliseconds, schedulingElapsedMilliseconds, messagingActionStart, roundtripStopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        Trace.TraceError("Unexpected error releasing message in subscription {1}:\r\n{0}", e, subscription);
                    }

                    callback(false);
                });
        }
    }
}

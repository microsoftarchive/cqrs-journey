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

namespace Infrastructure.Azure.Utils
{
    using System;
    using System.Diagnostics;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus.Messaging;

    public static class BrokeredMessageExtensions
    {
        private static readonly RetryPolicy FastRetryPolicy =
            new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(new Incremental(1, TimeSpan.Zero, TimeSpan.Zero) { FastFirstRetry = true });

        public static bool SafeComplete(this BrokeredMessage message)
        {
            return SafeMessagingAction(message.Complete, "An error occurred while completing message {0}. It will be reprocessed when the peek lock expires.", message.MessageId);
        }

        public static bool SafeAbandon(this BrokeredMessage message)
        {
            return SafeMessagingAction(message.Abandon, "An error occurred while abandoning message {0}. It will be reprocessed when the peek lock expires.", message.MessageId);
        }

        public static bool SafeDeadLetter(this BrokeredMessage message, string reason, string description)
        {
            return SafeMessagingAction(() => message.DeadLetter(reason, description), "An error occurred while dead-lettering message {0}. It will be reprocessed when the peek lock expires.", message.MessageId);
        }

        public static void SafeCompleteAsync(this BrokeredMessage message)
        {
            SafeMessagingActionAsync(
                ac => message.BeginComplete(ac, null),
                message.EndComplete,
                "An error occurred while completing message {0}. It will be reprocessed when the peek lock expires.",
                message.MessageId);
        }

        public static void SafeAbandonAsync(this BrokeredMessage message)
        {
            SafeMessagingActionAsync(
                ac => message.BeginAbandon(ac, null),
                message.EndAbandon,
                "An error occurred while abandoning message {0}. It will be reprocessed when the peek lock expires.",
                message.MessageId);
        }

        public static void SafeDeadLetterAsync(this BrokeredMessage message, string reason, string description)
        {
            SafeMessagingActionAsync(
                ac => message.BeginDeadLetter(reason, description, ac, null),
                message.EndDeadLetter,
                "An error occurred while dead-lettering message {0}. It will be reprocessed when the peek lock expires.",
                message.MessageId);
        }

        private static bool SafeMessagingAction(Action action, string actionErrorDescription, string messageId)
        {
            try
            {
                FastRetryPolicy.ExecuteAction(action);

                return true;
            }
            catch (MessageLockLostException)
            {
                Trace.TraceWarning(actionErrorDescription, messageId);
            }
            catch (MessagingException)
            {
                Trace.TraceWarning(actionErrorDescription, messageId);
            }
            catch (TimeoutException)
            {
                Trace.TraceWarning(actionErrorDescription, messageId);
            }

            return false;
        }

        private static void SafeMessagingActionAsync(Action<AsyncCallback> begin, Action<IAsyncResult> end, string actionErrorDescription, string messageId)
        {
            FastRetryPolicy.ExecuteAction(
                begin,
                end,
                () => { },
                e =>
                {
                    if (e is MessageLockLostException || e is MessagingException || e is TimeoutException)
                    {
                        Trace.TraceWarning(actionErrorDescription, messageId);
                    }
                    else
                    {
                        Trace.TraceError("Unexpected error releasing message:\r\n{0}", e);
                    }
                });
        }
    }
}

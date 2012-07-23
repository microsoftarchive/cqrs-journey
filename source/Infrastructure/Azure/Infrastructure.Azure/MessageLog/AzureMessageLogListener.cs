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

namespace Infrastructure.Azure.MessageLog
{
    using System;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;

    public class AzureMessageLogListener : IProcessor, IDisposable
    {
        private IAzureMessageLogWriter eventLog;
        private IMessageReceiver receiver;

        public AzureMessageLogListener(IAzureMessageLogWriter eventLog, IMessageReceiver receiver)
        {
            this.eventLog = eventLog;
            this.receiver = receiver;
        }

        public void SaveMessage(BrokeredMessage brokeredMessage)
        {
            this.eventLog.Save(brokeredMessage.ToMessageLogEntity());
        }

        public void Start()
        {
            this.receiver.Start(m => { this.SaveMessage(m); return MessageReleaseAction.CompleteMessage; });
        }

        public void Stop()
        {
            this.receiver.Stop();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                using (this.receiver as IDisposable) { }
            }
        }
    }
}

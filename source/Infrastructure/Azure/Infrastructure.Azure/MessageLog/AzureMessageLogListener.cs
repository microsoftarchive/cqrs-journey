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

namespace Infrastructure.Azure.MessageLog
{
    using System;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Utils;

    public class AzureMessageLogListener : IDisposable
    {
        private AzureMessageLogWriter eventLog;
        private IMessageReceiver receiver;

        public AzureMessageLogListener(AzureMessageLogWriter eventLog, IMessageReceiver receiver)
        {
            this.eventLog = eventLog;
            this.receiver = receiver;
            this.receiver.MessageReceived += SaveMessage;
        }

        public void SaveMessage(object sender, BrokeredMessageEventArgs args)
        {
            this.eventLog.Save(args.Message.ToMessageLogEntity());
            args.Message.SafeComplete();
        }

        public void Start()
        {
            this.receiver.Start();
        }

        public void Stop()
        {
            this.receiver.Stop();
        }

        public void Dispose()
        {
            var disposable = this.receiver as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }
}

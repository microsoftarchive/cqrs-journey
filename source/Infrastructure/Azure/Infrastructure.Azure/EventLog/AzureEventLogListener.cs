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

namespace Infrastructure.Azure.EventLog
{
    using Infrastructure.Azure.Messaging;

    public class AzureEventLogListener
    {
        private AzureEventLogWriter eventLog;
        private IMessageReceiver receiver;

        public AzureEventLogListener(AzureEventLogWriter eventLog, IMessageReceiver receiver)
        {
            this.eventLog = eventLog;
            this.receiver = receiver;

            this.receiver.MessageReceived += SaveMessage;
        }

        public void SaveMessage(object sender, BrokeredMessageEventArgs args)
        {
            this.eventLog.Save(args.Message.ToEventLogEntity());
        }
    }
}

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

namespace Infrastructure.Sql.MessageLog
{
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;

    /// <summary>
    /// The SQL version of the event log runs directly in-proc 
    /// and is implemented as an event and command handler instead of a 
    /// raw message listener.
    /// </summary>
    public class SqlMessageLogHandler : IEventHandler<IEvent>, ICommandHandler<ICommand>
    {
        private SqlMessageLog log;

        public SqlMessageLogHandler(SqlMessageLog log)
        {
            this.log = log;
        }

        public void Handle(IEvent @event)
        {
            this.log.Save(@event);
        }

        public void Handle(ICommand command)
        {
            this.log.Save(command);
        }
    }
}

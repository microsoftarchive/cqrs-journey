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

namespace MigrationToV2
{
    using System;
    using Conference;
    using Infrastructure.Messaging.Handling;
    using Registration.Handlers;
    using Registration.ReadModel.Implementation;

    // this class will forward the SeatCreated and SeatUpdated events to the generator when replaying, but will not
    // forward the order events (as we are not recreating the entire read model from the event log.
    public class PricedOrderViewModelUpdater :
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated>
    {
        private readonly PricedOrderViewModelGenerator innerGenerator;

        public PricedOrderViewModelUpdater(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.innerGenerator = new PricedOrderViewModelGenerator(contextFactory, null, null);
        }

        public void Handle(SeatCreated @event)
        {
            this.innerGenerator.Handle(@event);
        }

        public void Handle(SeatUpdated @event)
        {
            this.innerGenerator.Handle(@event);
        }
    }
}

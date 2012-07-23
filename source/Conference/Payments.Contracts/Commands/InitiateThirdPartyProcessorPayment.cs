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

namespace Payments.Contracts.Commands
{
    using System;
    using System.Collections.Generic;
    using Infrastructure.Messaging;

    public class InitiateThirdPartyProcessorPayment : ICommand
    {
        public class PaymentItem
        {
            public string Description { get; set; }

            public decimal Amount { get; set; }
        }

        public InitiateThirdPartyProcessorPayment()
        {
            this.Id = Guid.NewGuid();
            this.Items = new List<InitiateThirdPartyProcessorPayment.PaymentItem>();
        }

        public Guid Id { get; private set; }

        public Guid PaymentId { get; set; }

        public Guid PaymentSourceId { get; set; }

        public Guid ConferenceId { get; set; }

        public string Description { get; set; }

        public decimal TotalAmount { get; set; }

        public IList<InitiateThirdPartyProcessorPayment.PaymentItem> Items { get; private set; }
    }
}

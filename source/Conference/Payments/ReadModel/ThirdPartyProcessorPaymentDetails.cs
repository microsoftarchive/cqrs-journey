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

namespace Payments.ReadModel
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ThirdPartyProcessorPaymentDetails
    {
        public ThirdPartyProcessorPaymentDetails(Guid id, ThirdPartyProcessorPayment.States state, Guid paymentSourceId, string description, decimal totalAmount)
        {
            this.Id = id;
            this.State = state;
            this.PaymentSourceId = paymentSourceId;
            this.Description = description;
            this.TotalAmount = totalAmount;
        }

        protected ThirdPartyProcessorPaymentDetails()
        {
        }

        [Key]
        public Guid Id { get; private set; }

        public int StateValue { get; private set; }

        [NotMapped]
        public ThirdPartyProcessorPayment.States State
        {
            get { return (ThirdPartyProcessorPayment.States)this.StateValue; }
            set { this.StateValue = (int)value; }
        }

        public Guid PaymentSourceId { get; private set; }

        public string Description { get; private set; }

        public decimal TotalAmount { get; private set; }
    }
}

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

namespace Payments
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using Common;
    using Payments.Events;

    public class ThirdPartyProcessorPayment : IAggregateRoot, IEventPublisher
    {
        public enum States
        {
            Initiated = 0,
            Accepted = 1,
            Completed = 2,
            Rejected = 3
        }

        private List<IEvent> events = new List<IEvent>();

        public ThirdPartyProcessorPayment(Guid id, Guid sourceId, string description, double totalAmount, IEnumerable<ThidPartyProcessorPaymentItem> items)
            : this()
        {
            this.Id = id;
            this.SourceId = sourceId;
            this.Description = description;
            this.TotalAmount = totalAmount;
            this.Items.AddRange(items);

            this.AddEvent(new PaymentInitiated { PaymentId = id, SourceId = sourceId });
        }

        protected ThirdPartyProcessorPayment()
        {
            this.Items = new ObservableCollection<ThidPartyProcessorPaymentItem>();
        }

        public int StateValue { get; private set; }

        [NotMapped]
        public States State
        {
            get { return (States)this.StateValue; }
            internal set { this.StateValue = (int)value; }
        }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }

        public Guid Id { get; private set; }

        public Guid SourceId { get; private set; }

        public string Description { get; private set; }

        public double TotalAmount { get; private set; }

        public virtual ICollection<ThidPartyProcessorPaymentItem> Items { get; private set; }

        public void Complete()
        {
            if (this.State != States.Initiated)
            {
                throw new InvalidOperationException();
            }

            this.State = States.Completed;
            this.AddEvent(new PaymentCompleted { PaymentId = this.Id, SourceId = this.SourceId });
        }

        public void Cancel()
        {
            if (this.State != States.Initiated)
            {
                throw new InvalidOperationException();
            }

            this.State = States.Rejected;
            this.AddEvent(new PaymentRejected { PaymentId = this.Id, SourceId = this.SourceId });
        }

        protected void AddEvent(IEvent @event)
        {
            this.events.Add(@event);
        }
    }
}

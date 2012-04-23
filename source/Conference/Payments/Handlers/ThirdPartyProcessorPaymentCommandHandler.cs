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

namespace Payments.Handlers
{
    using System;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Relational;
    using Payments.Contracts.Commands;

    public class ThirdPartyProcessorPaymentCommandHandler :
        ICommandHandler<InitiateThirdPartyProcessorPayment>,
        ICommandHandler<CompleteThirdPartyProcessorPayment>,
        ICommandHandler<CancelThirdPartyProcessorPayment>
    {
        private Func<IDataContext<ThirdPartyProcessorPayment>> contextFactory;

        public ThirdPartyProcessorPaymentCommandHandler(Func<IDataContext<ThirdPartyProcessorPayment>> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(InitiateThirdPartyProcessorPayment command)
        {
            var repository = this.contextFactory();

            using (repository as IDisposable)
            {
                var items = command.Items.Select(t => new ThidPartyProcessorPaymentItem(t.Description, t.Amount)).ToList();
                var payment = new ThirdPartyProcessorPayment(command.PaymentId, command.PaymentSourceId, command.Description, command.TotalAmount, items);

                repository.Save(payment);
            }
        }

        public void Handle(CompleteThirdPartyProcessorPayment command)
        {
            var repository = this.contextFactory();

            using (repository as IDisposable)
            {
                var payment = repository.Find(command.PaymentId);

                if (payment != null)
                {
                    payment.Complete();
                    repository.Save(payment);
                }
            }
        }

        public void Handle(CancelThirdPartyProcessorPayment command)
        {
            var repository = this.contextFactory();

            using (repository as IDisposable)
            {
                var payment = repository.Find(command.PaymentId);

                if (payment != null)
                {
                    payment.Cancel();
                    repository.Save(payment);
                }
            }
        }
    }
}

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

namespace Payments.ReadModel.Implementation
{
    using System.Data.Entity;
    using System.Linq;
    using Payments.Database;

    public class PaymentsReadDbContextInitializer : IDatabaseInitializer<PaymentsDbContext>
    {
        // NOTE: we initialize the same OrmRepository for both because we happen to 
        // persist the views in the same database. This is not required and could be 
        // a separate one if we weren't using SQL Views to drive them.
        private IDatabaseInitializer<PaymentsDbContext> innerInitializer;

        public PaymentsReadDbContextInitializer(IDatabaseInitializer<PaymentsDbContext> innerInitializer)
        {
            this.innerInitializer = innerInitializer;
        }

        public void InitializeDatabase(PaymentsDbContext context)
        {
            this.innerInitializer.InitializeDatabase(context);

            if (!context.Database.SqlQuery<int>("SELECT object_id FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[ThirdPartyProcessorPaymentDetailsView]')").Any())
            {
                context.Database.ExecuteSqlCommand(@"
CREATE VIEW [dbo].[ThirdPartyProcessorPaymentDetailsView]
AS
SELECT     
    dbo.ThirdPartyProcessorPayments.Id AS Id, 
    dbo.ThirdPartyProcessorPayments.StateValue as StateValue,
    dbo.ThirdPartyProcessorPayments.SourceId as SourceId,
    dbo.ThirdPartyProcessorPayments.Description as Description,
    dbo.ThirdPartyProcessorPayments.TotalAmount as TotalAmount
FROM dbo.ThirdPartyProcessorPayments");

                //                context.Database.ExecuteSqlCommand(@"
                //CREATE VIEW [dbo].[PaymentItemsView]
                //AS
                //SELECT     
                //    dbo.PaymentItems.Id AS Id, 
                //    dbo.PaymentItems.Payment_Id AS ThirdPartyProcessorPaymentDetailsView_Id, 
                //    dbo.PaymentItems.SeatType as SeatType,
                //    dbo.PaymentItems.Quantity as Quantity
                //FROM dbo.PaymentItems");
            }

            context.SaveChanges();
        }
    }
}

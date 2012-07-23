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

            if (!context.Database.SqlQuery<int>("SELECT object_id FROM sys.views WHERE object_id = OBJECT_ID(N'[" + PaymentsReadDbContext.SchemaName + "].[ThirdPartyProcessorPaymentDetailsView]')").Any())
            {
                CreateViews(context);
            }

            context.SaveChanges();
        }

        public static void CreateViews(DbContext context)
        {
            context.Database.ExecuteSqlCommand(@"
CREATE VIEW " + PaymentsReadDbContext.SchemaName + @".[ThirdPartyProcessorPaymentDetailsView]
AS
SELECT     
    Id AS Id, 
    StateValue as StateValue,
    PaymentSourceId as PaymentSourceId,
    Description as Description,
    TotalAmount as TotalAmount
FROM " + PaymentsDbContext.SchemaName + ".ThirdPartyProcessorPayments");

            //                context.Database.ExecuteSqlCommand(@"
            //CREATE VIEW [Payments].[PaymentItemsView]
            //AS
            //SELECT     
            //    Payments.PaymentItems.Id AS Id, 
            //    Payments.PaymentItems.Payment_Id AS ThirdPartyProcessorPaymentDetailsView_Id, 
            //    Payments.PaymentItems.SeatType as SeatType,
            //    Payments.PaymentItems.Quantity as Quantity
            //FROM Payments.PaymentItems");
        }
    }
}

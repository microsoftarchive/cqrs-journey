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

namespace Conference.Web.Public
{
    using System.Web.Mvc;
    using System.Web.Routing;

    public static class AppRoutes
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                "Home",
                string.Empty,
                new { controller = "Default", action = "Index" });

            routes.MapRoute(
                "ViewConference",
                "{conferenceCode}",
                new { controller = "Conference", action = "Display" });

            routes.MapRoute(
                "RegisterStart",
                "{conferenceCode}/register",
                new { controller = "Registration", action = "StartRegistration" });

            routes.MapRoute(
                "RegisterRegistrantDetails",
                "{conferenceCode}/registrant",
                new { controller = "Registration", action = "SpecifyRegistrantDetails" });

            routes.MapRoute(
                "RegisterChoosePayment",
                "{conferenceCode}/payment",
                new { controller = "Registration", action = "SpecifyPaymentDetails" });

            routes.MapRoute(
                "RegisterTransactionCompleted",
                "{conferenceCode}/completed",
                new { controller = "Registration", action = "TransactionCompleted" });

            routes.MapRoute(
                "RegisterConfirmation",
                "{conferenceCode}/confirmation",
                new { controller = "Registration", action = "ThankYou" });

            routes.MapRoute(
                "PaymentDisplay",
                "{conferenceCode}/payment-fake",
                new { controller = "Payment", action = "Display" });

            routes.MapRoute(
                "PaymentAccept",
                "{conferenceCode}/payment-accept",
                new { controller = "Payment", action = "AcceptPayment" });

            routes.MapRoute(
                "PaymentReject",
                "{conferenceCode}/payment-reject",
                new { controller = "Payment", action = "RejectPayment" });

            routes.MapRoute(
                "OrderFind",
                "{conferenceCode}/order/find",
                new { controller = "Order", action = "Find" });

            routes.MapRoute(
                "OrderDisplay",
                "{conferenceCode}/order/{orderId}",
                new { controller = "Order", action = "Display" });

            routes.MapRoute(
                "ShowOrder",
                "{conferenceCode}/order",
                new { controller = "Registration", Action = "DisplayOrderStatus" });
        }
    }
}

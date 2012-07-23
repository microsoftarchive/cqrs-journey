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

namespace Conference.Web.Public
{
    using System.Web.Mvc;
    using System.Web.Routing;

    public static class AppRoutes
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

            routes.MapRoute(
                "Home",
                string.Empty,
                new { controller = "Default", action = "Index" });

            // Registration routes

            routes.MapRoute(
                "ViewConference",
                "{conferenceCode}/",
                new { controller = "Conference", action = "Display" });

            routes.MapRoute(
                "RegisterStart",
                "{conferenceCode}/register",
                new { controller = "Registration", action = "StartRegistration" });

            routes.MapRoute(
                "RegisterRegistrantDetails",
                "{conferenceCode}/registrant",
                new { controller = "Registration", action = "SpecifyRegistrantAndPaymentDetails" });

            routes.MapRoute(
                "StartPayment",
                "{conferenceCode}/pay",
                new { controller = "Registration", action = "StartPayment" });

            routes.MapRoute(
                "ExpiredOrder",
                "{conferenceCode}/expired",
                new { controller = "Registration", action = "ShowExpiredOrder" });

            routes.MapRoute(
                "RegisterConfirmation",
                "{conferenceCode}/confirmation",
                new { controller = "Registration", action = "ThankYou" });

            routes.MapRoute(
                "OrderFind",
                "{conferenceCode}/order/find",
                new { controller = "Order", action = "Find" });

            routes.MapRoute(
                "AssignSeats",
                "{conferenceCode}/order/{orderId}/seats",
                new { controller = "Order", action = "AssignSeats" });
            
            routes.MapRoute(
                "AssignSeatsWithoutAssignmentsId",
                "{conferenceCode}/order/{orderId}/seats-redirect",
                new { controller = "Order", action = "AssignSeatsForOrder" });
            
            routes.MapRoute(
                "OrderDisplay",
                "{conferenceCode}/order/{orderId}",
                new { controller = "Order", action = "Display" });

            routes.MapRoute(
                "InitiateThirdPartyPayment",
                "{conferenceCode}/third-party-payment",
                new { controller = "Payment", action = "ThirdPartyProcessorPayment" });

            routes.MapRoute(
                "PaymentAccept",
                "{conferenceCode}/third-party-payment-accept",
                new { controller = "Payment", action = "ThirdPartyProcessorPaymentAccepted" });

            routes.MapRoute(
                "PaymentReject",
                "{conferenceCode}/third-party-payment-reject",
                new { controller = "Payment", action = "ThirdPartyProcessorPaymentRejected" });
        }
    }
}

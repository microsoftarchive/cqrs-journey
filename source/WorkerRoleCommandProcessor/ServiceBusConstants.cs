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

namespace WorkerRoleCommandProcessor
{
	public static class Topics
	{
		public static class Commands
		{
			/// <summary>
			/// conference/commands
			/// </summary>
			public const string Path = "conference/commands";

			public static class Subscriptions
			{
				/// <summary>
				/// sessionless
				/// </summary>
				public const string Sessionless = "sessionless";
				/// <summary>
				/// seatsavailability
				/// </summary>
				public const string Seatsavailability = "seatsavailability";
				/// <summary>
				/// log
				/// </summary>
				public const string Log = "log";
			}
		}

		public static class Events
		{
			/// <summary>
			/// conference/events
			/// </summary>
			public const string Path = "conference/events";

			public static class Subscriptions
			{
				/// <summary>
				/// log
				/// </summary>
				public const string Log = "log";
				/// <summary>
				/// Registration.RegistrationPMNextSteps
				/// </summary>
				public const string RegistrationPMNextSteps = "Registration.RegistrationPMNextSteps";
				/// <summary>
				/// Registration.PricedOrderViewModelGeneratorV3
				/// </summary>
				public const string PricedOrderViewModelGeneratorV3 = "Registration.PricedOrderViewModelGeneratorV3";
				/// <summary>
				/// Registration.ConferenceViewModelGenerator
				/// </summary>
				public const string ConferenceViewModelGenerator = "Registration.ConferenceViewModelGenerator";
			}
		}

		public static class EventsOrders
		{
			/// <summary>
			/// conference/eventsOrders
			/// </summary>
			public const string Path = "conference/eventsOrders";

			public static class Subscriptions
			{
				/// <summary>
				/// logOrders
				/// </summary>
				public const string LogOrders = "logOrders";
				/// <summary>
				/// Registration.RegistrationPMOrderPlacedOrders
				/// </summary>
				public const string RegistrationPMOrderPlacedOrders = "Registration.RegistrationPMOrderPlacedOrders";
				/// <summary>
				/// Registration.RegistrationPMNextStepsOrders
				/// </summary>
				public const string RegistrationPMNextStepsOrders = "Registration.RegistrationPMNextStepsOrders";
				/// <summary>
				/// Registration.OrderViewModelGeneratorOrders
				/// </summary>
				public const string OrderViewModelGeneratorOrders = "Registration.OrderViewModelGeneratorOrders";
				/// <summary>
				/// Registration.PricedOrderViewModelOrders
				/// </summary>
				public const string PricedOrderViewModelOrders = "Registration.PricedOrderViewModelOrders";
				/// <summary>
				/// Registration.SeatAssignmentsViewModelOrders
				/// </summary>
				public const string SeatAssignmentsViewModelOrders = "Registration.SeatAssignmentsViewModelOrders";
				/// <summary>
				/// Registration.SeatAssignmentsHandlerOrders
				/// </summary>
				public const string SeatAssignmentsHandlerOrders = "Registration.SeatAssignmentsHandlerOrders";
				/// <summary>
				/// Conference.OrderEventHandlerOrders
				/// </summary>
				public const string OrderEventHandlerOrders = "Conference.OrderEventHandlerOrders";
			}
		}

		public static class EventsAvailability
		{
			/// <summary>
			/// conference/eventsAvailability
			/// </summary>
			public const string Path = "conference/eventsAvailability";

			public static class Subscriptions
			{
				/// <summary>
				/// logAvail
				/// </summary>
				public const string LogAvail = "logAvail";
				/// <summary>
				/// Registration.RegistrationPMNextStepsAvail
				/// </summary>
				public const string RegistrationPMNextStepsAvail = "Registration.RegistrationPMNextStepsAvail";
				/// <summary>
				/// Registration.ConferenceViewModelAvail
				/// </summary>
				public const string ConferenceViewModelAvail = "Registration.ConferenceViewModelAvail";
			}
		}

	}
}

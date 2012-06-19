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
                public const string SeatsAvailability = "seatsavailability";
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
				/// Registration.RegistrationProcessRouter
				/// </summary>
				public const string RegistrationProcessRouterV3 = "Registration.RegistrationProcessRouterV3";
				/// <summary>
				/// Registration.OrderViewModelGenerator
				/// </summary>
				public const string OrderViewModelGenerator = "Registration.OrderViewModelGenerator";
				/// <summary>
				/// Registration.PricedOrderViewModelGenerator
				/// </summary>
				public const string PricedOrderViewModelGenerator = "Registration.PricedOrderViewModelGenerator";
				/// <summary>
				/// Registration.ConferenceViewModelGenerator
				/// </summary>
				public const string ConferenceViewModelGenerator = "Registration.ConferenceViewModelGenerator";
				/// <summary>
				/// Registration.SeatAssignmentsViewModelGenerator
				/// </summary>
				public const string SeatAssignmentsViewModelGenerator = "Registration.SeatAssignmentsViewModelGenerator";
				/// <summary>
				/// Registration.SeatAssignmentsHandler
				/// </summary>
				public const string SeatAssignmentsHandler = "Registration.SeatAssignmentsHandler";
				/// <summary>
				/// Conference.OrderEventHandler
				/// </summary>
				public const string OrderEventHandler = "Conference.OrderEventHandler";
			}
		}

	}
}

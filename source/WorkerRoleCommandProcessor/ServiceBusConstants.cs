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
				/// Registration.RegistrationPMOrderPlaced
				/// </summary>
				public const string RegistrationPMOrderPlaced = "Registration.RegistrationPMOrderPlaced";
				/// <summary>
				/// Registration.RegistrationPMNextSteps
				/// </summary>
				public const string RegistrationPMNextSteps = "Registration.RegistrationPMNextSteps";
				/// <summary>
				/// Registration.OrderViewModelGeneratorV3
				/// </summary>
				public const string OrderViewModelGeneratorV3 = "Registration.OrderViewModelGeneratorV3";
				/// <summary>
				/// Registration.PricedOrderViewModelGeneratorV3
				/// </summary>
				public const string PricedOrderViewModelGeneratorV3 = "Registration.PricedOrderViewModelGeneratorV3";
				/// <summary>
				/// Registration.ConferenceViewModelGenerator
				/// </summary>
				public const string ConferenceViewModelGenerator = "Registration.ConferenceViewModelGenerator";
				/// <summary>
				/// Registration.SeatAssignmentsViewModelGeneratorV3
				/// </summary>
				public const string SeatAssignmentsViewModelGeneratorV3 = "Registration.SeatAssignmentsViewModelGeneratorV3";
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

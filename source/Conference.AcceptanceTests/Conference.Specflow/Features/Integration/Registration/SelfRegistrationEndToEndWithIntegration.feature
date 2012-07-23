# ==============================================================================================================
# Microsoft patterns & practices
# CQRS Journey project
# ==============================================================================================================
# ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
# http://go.microsoft.com/fwlink/p/?LinkID=258575
# Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
# with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software distributed under the License is 
# distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
# See the License for the specific language governing permissions and limitations under the License.
# ==============================================================================================================

@SelfRegistrationEndToEndWithIntegration
@NoWatiN
Feature: Self Registrant end to end scenario for making a Registration for a Conference site with Domain Commands and Events
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference, pay for the Registration Order and associate myself with the paid Order automatically


Scenario: Make a reservation with the selected Order Items
Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	# command:RegisterToConference
When the command to register the selected Order Items is sent 
	# event: OrderPlaced
Then the event for Order placed is emitted
	# command: MakeSeatReservation
And the command for reserving the selected Seats is received
	# event: SeatsReserved
And the event for reserving the selected Seats is emitted
	# event: OrderReservationCompleted 
And the event for completing the Order reservation is emitted
	# event: OrderTotalsCalculated
And the event for calculating the total of $249 is emitted



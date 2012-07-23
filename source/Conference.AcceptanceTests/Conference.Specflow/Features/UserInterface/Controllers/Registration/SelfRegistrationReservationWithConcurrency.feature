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

Feature: Self-Registrant scenarios for making a Registration for a Conference site where multiple Registrants make simultaneous reservations
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available where other Registrants may also be interested

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type     | rate | quota |
	| CQRS Workshop | $500 | 10    |


@NoWatiN
Scenario: Many Registrants try to reserve the same Order Item and not all of them get the registration	
	When 20 Registrants select these Order Items
	| seat type     | quantity |
	| CQRS Workshop | 1        |
	Then only 10 Registrants get confirmed registrations for the selected Order Items

# ==============================================================================================================
# Microsoft patterns & practices
# CQRS Journey project
# ==============================================================================================================
# ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
# http://cqrsjourney.github.com/contributors/members
# Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
# with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software distributed under the License is 
# distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
# See the License for the specific language governing permissions and limitations under the License.
# ==============================================================================================================

Feature: Registrant workflow for registering a group of Attendees for a conference (sad path)
	In order to register for conference a group of Attendees
	As a Registrant
	I want to be able to select Order Items from one or many available Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference with the slug code GroupRegE2Esad
	| seat type                 | rate | quota |
	| General admission         | $199 | 2     |
	| CQRS Workshop             | $500 | 2     |
	| Additional cocktail party | $50  | 2     |


#Initial state	: 3 available items, 2 selected (q=4)
#End state		: 2 reserved and 1 offered waitlisted
Scenario: All the Order Items are available, then some get waitlisted and some reserved
	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 2        |
	| Additional cocktail party | 2        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type         | quantity |
	| General admission | 1        |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type         | quantity |
	| General admission | 1        |
	And these Order Items should be reserved
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 2        |
	And the total should read $299
	And the countdown started


# Next release
@Ignore
Scenario: Allocate some purchased Seats for a group
Given the ConfirmSuccessfulRegistration
And the order access code is 6789
And the Registrant assign the group purchased Seats to attendees as following
	| First name | Last name | email address     | Seat type         |
	| William    | Weber     | William@Weber.com | General admission |
And leave unassigned these individual purchased seats
	| First name | Last name | email address | Seat type                 |
	| Mani       | Kris      | Mani@Kris.com | Additional cocktail party |
Then the Registrant should get a Seat Assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address     | Seat type                        |
	| 6789-1      | William@Weber.com | General admission                |






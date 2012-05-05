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
	| General admission         | $199 | 20    |
	| CQRS Workshop             | $500 | 20    |
	| Additional cocktail party | $50  | 20    |


#Initial state	: 3 selected and 1 none available and 1 partially available
#End state		: 1 reserved, 1 partially reserved and 1 not reserved  
 Scenario: All the Order Items are selected and none are available, then none get reserved	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 | quantity |
	| General admission         | 18       |
	| CQRS Workshop             | 20       |
	| Additional cocktail party | 10       |
	And the Registrant proceed to make the Reservation with seats already reserved 		
	And the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                    |
	| General admission         | 2        | Could not reserve all the requested seats. |
	| CQRS Workshop             | 0        | Could not reserve all the requested seats. |
	| Additional cocktail party | 2        |                                            |
	And the total should read $498
	When the Registrant proceed to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
	| seat type                 | quantity |
	| General admission         | 2        |
	| Additional cocktail party | 2        |
	And the total should read $498
	And the countdown started


Scenario: Allocate some purchased Seats
 	Given the selected Order Items
	| seat type                 | quantity |
	| Additional cocktail party | 4        |
	And the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	And the Registrant proceed to confirm the payment
    And the message 'Thank you' will show up
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| Additional cocktail party | 4        |
	When the Registrant assign these seats
	| seat type                 | first name | last name | email address       |
	| Additional cocktail party | Mani       | Kris      | Mani@Kris.com       |
	| Additional cocktail party | Jim        | Gregory   | Jim@Gregory.com     |
	Then these seats are assigned
	| seat type                 | quantity |
	| Additional cocktail party | 4        |






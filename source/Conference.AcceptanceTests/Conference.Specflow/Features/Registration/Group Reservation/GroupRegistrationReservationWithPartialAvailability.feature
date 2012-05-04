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

Feature: Registrant scenarios for registering a group of Attendees for a conference when few Seats are available in all the Seat Types
	In order to register for conference a group of Attendees
	As a Registrant
    I want to be able to select Order Items from one or many of the available and or waitlisted Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference with the slug code GroupRegPartial
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |


#Initial state	: 3 selected and none available
#End state		: 3 not reserved  
 Scenario: All the Order Items are selected and none are available, then none get reserved	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 |
	| General admission         |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceed to make the Reservation with seats already reserved 		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                |
	| General admission         | 0        | Could not reserve all requested seats. |
	| CQRS Workshop             | 0        | Could not reserve all requested seats. |
	| Additional cocktail party | 0        | Could not reserve all requested seats. |
	And the countdown started


#Initial state	: 3 selected and two get unavailable
#End state		: 1 reserved and 2 not get reserved  
 Scenario: All the Order Items are selected, one partially available and one not available, then one get reserved, one partially reserved and one not	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 11       |
	| Additional cocktail party | 1        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 | quantity |
	| CQRS Workshop             | 90       |
	| Additional cocktail party |          |
	When the Registrant proceed to make the Reservation with seats already reserved 		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                |
	| General admission         | 3        |                                        |
	| CQRS Workshop             | 10       | Could not reserve all requested seats. |
	| Additional cocktail party | 0        | Could not reserve all requested seats. |
	And the countdown started



#Initial state	: 3 selected and 3 get partially unavailable (1 full)
#End state		: 2 reserved (1 partially) and 1 not get reserved  
 Scenario: All the Order Items are selected, two are partially available and one none available, then two get partially reserved and one not	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 7        |
	| CQRS Workshop             | 12       |
	| Additional cocktail party | 9        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 | quantity |
	| General admission         |		   |
	| CQRS Workshop             | 90       |
	| Additional cocktail party | 90       |
	And the Registrant proceed to make the Reservation with seats already reserved 		
	And the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                |
	| General admission         | 0        | Could not reserve all requested seats. |
	| CQRS Workshop             | 10       |                                        |
	| Additional cocktail party | 9        |                                        |
	And the total should read $5450
	When the Registrant proceed to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
	| seat type                 | quantity |
	| CQRS Workshop             | 10       |
	| Additional cocktail party | 9        |
	And the total should read $5450
	And the countdown started












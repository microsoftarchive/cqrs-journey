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

Feature: Self Registrant scenarios for making a Reservation for a Conference site with Order Items partially available
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available and or waitlisted Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference with the slug code SelfRegPartial
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |


#Initial state	: 3 selected and none available
#End state		: 3 not reserved  
 Scenario: All the Order Items are selected and none are available, then none get reserved	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 |
	| General admission         |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceed to make the Reservation with seats already reserved 		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                    |
	| General admission         | 0        | Could not reserve all the requested seats. |
	| CQRS Workshop             | 0        | Could not reserve all the requested seats. |
	| Additional cocktail party | 0        | Could not reserve all the requested seats. |
	And the countdown started


#Initial state	: 3 selected and two get reserved
#End state		: 1 reserved and 2 not get reserved  
 Scenario: All the Order Items are selected and two not available, then one get reserved and two not	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceed to make the Reservation with seats already reserved 		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                    |
	| General admission         | 1        |                                            |
	| CQRS Workshop             | 0        | Could not reserve all the requested seats. |
	| Additional cocktail party | 0        | Could not reserve all the requested seats. |
	And the countdown started


#Initial state	: 3 selected and 1 get partially reserved and 1 get all reserved
#End state		: 2 reserved and 1 not get reserved  
 Scenario: All the Order Items are selected, one is partially available and one none available, then two get reserved and one not	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 | quantity |
	| CQRS Workshop             | 10       |
	| Additional cocktail party |          |
	And the Registrant proceed to make the Reservation with seats already reserved 		
	And the Registrant is offered to select any of these available seats
	| seat type                 | selected | message                                    |
	| General admission         | 1        |                                            |
	| CQRS Workshop             | 1        |                                            |
	| Additional cocktail party | 0        | Could not reserve all the requested seats. |
	And the total should read $699
	When the Registrant proceed to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
		| seat type         | quantity |
		| General admission | 1        |
		| CQRS Workshop     | 1        |
	And the total should read $699
	And the countdown started


#Initial state	: 1 available, 2 waitlisted but only 2w selected
#End state		: 2 waitlisted confirmed  
#Next release
@Ignore
Scenario: 1 order item is available, 2 are waitlisted and 2 are selected, then 2 get confirmed	
	Given the list of available Order Items selected by the Registrant
	| seat type         | quantity |
	| General admission | 0        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	When the Registrant proceed to make the Reservation					
	Then these order itmes get confirmed being waitlisted
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |


#Initial state	: 1 available, 2 waitlisted and only 1a selected
#End state		: 1 reserved 
#Next release
@Ignore
Scenario: 1 order item is available,  2 are waitlisted and 1 available is selected, then only 1 get reserved	
	Given the list of available Order Items selected by the Registrant
	| seat type         | quantity |
	| General admission | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                 | quantity |
	| CQRS Workshop             | 0        |
	| Additional cocktail party | 0        |
	When the Registrant proceed to make the Reservation					
	Then these order items get reserved
	| seat type         | quantity |
	| General admission | 1        |


#Initial state	: 1 available, 2 waitlisted and 1a & 1w selected
#End state		: 1 reserved,  1 waitlisted confirmed  
#Next release
@Ignore
Scenario: 1 order item is available, 2 are waitlisted, 1 available and 1 waitlisted are selected, then 1 get reserved and 1 get waitlisted	
	Given the list of available Order Items selected by the Registrant
	| seat type         | quantity |
	| General admission | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 0        |
	When the Registrant proceed to make the Reservation					
	Then these order itmes get confirmed being waitlisted
	| seat type     | quantity |
	| CQRS Workshop | 1        |
	And these other order items get reserved
	| seat type         | quantity |
	| General admission | 1        |




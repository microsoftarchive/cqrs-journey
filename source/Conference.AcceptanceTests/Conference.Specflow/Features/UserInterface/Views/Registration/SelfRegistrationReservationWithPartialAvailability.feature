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

Feature: Self Registrant scenarios for making a Reservation for a Conference site with Order Items partially available
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available and or waitlisted Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 20    |
	| CQRS Workshop             | $500 | 20    |
	| Additional cocktail party | $50  | 20    |


#Initial state	: 3 selected and none available
#End state		: 3 not reserved  
 Scenario: All the Order Items are selected and none are available, then none get reserved	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Seat Types become unavailable before the Registrant makes the reservation
	| seat type                 |
	| General admission         |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceeds to make the Reservation with seats already reserved 		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message  |
	| General admission         |          | Sold out |
	| CQRS Workshop             |          | Sold out |
	| Additional cocktail party |          | Sold out |


#Initial state	: 3 selected and two get reserved
#End state		: 1 reserved and 2 not get reserved  
 Scenario: All the Order Items are selected and two are not available, then one get reserved and two not	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Seat Types become unavailable before the Registrant makes the reservation
	| seat type                 |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceeds to make the Reservation with seats already reserved 		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message  |
	| General admission         | 0        |          |
	| CQRS Workshop             |          | Sold out |
	| Additional cocktail party |          | Sold out |
	#And the countdown is started


#Initial state	: 3 selected and 1 get partially reserved and 1 get all reserved
#End state		: 2 reserved and 1 not get reserved  
 Scenario: All the Order Items are selected, one is partially available and one none available, then two get reserved and one not	
 	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Seat Types become unavailable before the Registrant makes the reservation
	| seat type                 | quantity |
	| CQRS Workshop             | 10       |
	| Additional cocktail party |          |
	And the Registrant proceeds to make the Reservation with seats already reserved 		
	And the Registrant is offered to select any of these available seats
	| seat type                 | selected | message  |
	| General admission         | 0        |          |
	| CQRS Workshop             | 0        |          |
	| Additional cocktail party |          | Sold out |
	And the selected Order Items
	| seat type         | quantity |
	| General admission | 1        |
	| CQRS Workshop     | 1        |
	And the total should read $699
	When the Registrant proceeds to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
		| seat type         | quantity |
		| General admission | 1        |
		| CQRS Workshop     | 1        |
	And the total should read $699
	And the countdown is started
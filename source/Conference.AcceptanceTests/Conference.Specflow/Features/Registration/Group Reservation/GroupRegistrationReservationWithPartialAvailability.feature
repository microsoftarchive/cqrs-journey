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

#1
#Initial state	: 3 waitlisted and 3 selected
#End state		: 3 waitlisted confirmed  
#Next release
@Ignore
Scenario: All the Order Items are offered to be waitlisted and all are selected, then all get confirmed	
	Given the list of Order Items offered to be waitlisted and selected by the Registrant
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	When the Registrant proceed to make the Reservation			
	Then these Order Itmes get confirmed being waitlisted
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |

#2
#Initial state	: 2 available items and 1 waitlisted, 3 selected
#End state		: 2 reserved, 2 waitlisted
#Next release
@Ignore
Scenario: 2 the Order Items are available and 1 waitlisted, 1 becomes partially available,
	      then 2 are partially offered to get waitlisted and 2 get reserved
	Given the selected available Order Items
	| seat type         | quantity |
	| General admission | 7        |
	| CQRS Workshop     | 2        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| Additional cocktail party        | 5        |	
	And these Seat Types becomes partially unavailable before the Registrant make the reservation
	| seat type         |
	| General admission |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| Additional cocktail party | 5        |
	And These other Order Items get reserved
	| seat type         | quantity |
	| General admission | 4        |
	| CQRS Workshop     | 2        |
	And And the total should read $1796











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

Feature: Self Registrant waitlisting scenarios for making a Reservation for a Conference site with Order Items partially available
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available and or waitlisted Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |


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
	When the Registrant proceeds to make the Reservation					
	Then these order items get confirmed being waitlisted
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |


#Initial state	: 1 available, 2 waitlisted and only 1a selected
#End state		: 1 reserved 
#Next release
@Ignore
Scenario: 1 order item is available,  2 are waitlisted and 1 available is selected, then only 1 gets reserved	
	Given the list of available Order Items selected by the Registrant
	| seat type         | quantity |
	| General admission | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                 | quantity |
	| CQRS Workshop             | 0        |
	| Additional cocktail party | 0        |
	When the Registrant proceeds to make the Reservation					
	Then these order items get reserved
	| seat type         | quantity |
	| General admission | 1        |


#Initial state	: 1 available, 2 waitlisted and 1a & 1w selected
#End state		: 1 reserved,  1 waitlisted confirmed  
#Next release
@Ignore
Scenario: 1 order item is available, 2 are waitlisted, 1 available and 1 waitlisted are selected, then 1 gets reserved and 1 gets waitlisted	
	Given the list of available Order Items selected by the Registrant
	| seat type         | quantity |
	| General admission | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 0        |
	When the Registrant proceeds to make the Reservation					
	Then these order items get confirmed being waitlisted
	| seat type     | quantity |
	| CQRS Workshop | 1        |
	And these other order items get reserved
	| seat type         | quantity |
	| General admission | 1        |

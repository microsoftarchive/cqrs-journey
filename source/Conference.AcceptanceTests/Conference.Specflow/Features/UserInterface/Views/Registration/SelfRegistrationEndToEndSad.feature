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

Feature: Self Registrant end to end scenario for making a Registration for a Conference (sad path)
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference, pay for the Registration Order and associate myself with the paid Order automatically

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 10    |
	| CQRS Workshop             | $500 | 10    |
	| Additional cocktail party | $50  | 10    |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |


Scenario: No selected Seat Type
	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 0        |
	| CQRS Workshop             | 0        |
	| Additional cocktail party | 0        |
	When the Registrant proceed to make the Reservation with no selected seats
	Then the message 'One or more items are required' will show up


#Initial state	: 3 available
#End state		: 2 waitlisted, 1 reserved
Scenario: All Seat Types are available, one get reserved and two get waitlisted
	Given these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceed to make the Reservation with seats already reserved		
	Then the Registrant is offered to select any of these available seats
	| seat type                 | selected | message  |
	| CQRS Workshop             |          | Sold out |
	| Additional cocktail party |          | Sold out |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	And these Order Items should be reserved
	| seat type                        | quantity |
	| General admission                | 1		  |
	And the total should read $199


Scenario: Checkout:Registrant Invalid Details
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address        |
	| Gregory    |           | gregoryweber@invalid |
	When the Registrant proceed to Checkout:Payment
	Then the message 'The LastName field is required.' will show up 	


Scenario: Checkout:Payment with cancellation
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	When the Registrant proceed to cancel the payment
    Then the payment selection page will show up 	


Scenario: Partiall Seats allocation
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	And the Registrant proceed to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	When the Registrant assign these seats
	| seat type                 | first name | last name | email address            |
	| General admission         | Gregory    | Weber     | gregoryweber@contoso.com |
	| Additional cocktail party | Gregory    | Weber     | gregoryweber@contoso.com |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |




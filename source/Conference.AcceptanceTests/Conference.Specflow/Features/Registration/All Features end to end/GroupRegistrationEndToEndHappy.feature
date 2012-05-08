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

Feature: Registrant workflow for registering a group of Attendees for a conference (happy path)
	In order to register for conference a group of Attendees
	As a Registrant
	I want to be able to select Order Items from one or many available Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |

#1
#Initial state	: 3 available items, 3 selected
#End state		: 3 reserved	
Scenario: All the Order Items are available and all get selected, then all get reserved
	When the Registrant proceed to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
		| seat type                 | quantity |
		| General admission         | 3        |
		| CQRS Workshop             | 1        |
		| Additional cocktail party | 2        |
	And the total should read $1197
	And the countdown started	

Scenario: Checkout:Registrant Details
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address         |
	| William    | Weber     | William@Weber.com     |
	When the Registrant proceed to Checkout:Payment
	Then the payment options should be offered for a total of $1197

Scenario: Checkout:Payment and sucessfull Order completed
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address         |
	| William    | Weber     | William@Weber.com     |
	And the Registrant proceed to Checkout:Payment
	When the Registrant proceed to confirm the payment
    Then the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |


Scenario: Allocate all purchased Seats
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	And the Registrant proceed to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	When the Registrant assign these seats
	| seat type                 | first name | last name | email address       |
	| General admission         | William    | Weber     | William@Weber.com   |
	| General admission         | Gregory    | Doe       | GregoryDoe@live.com |
	| General admission         | Oliver     | Weber     | Oliver@Weber.com    |
	| CQRS Workshop             | Tim        | Martin    | Tim@Martin.com      |
	| Additional cocktail party | Mani       | Kris      | Mani@Kris.com       |
	| Additional cocktail party | Jim        | Gregory   | Jim@Gregory.com     |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
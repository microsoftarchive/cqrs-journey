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

Feature: Registrant workflow for registering a group of Attendees for a conference (happy path)
	In order to register a group of Attendees for conference
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
	When the Registrant proceeds to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
		| seat type                 | quantity |
		| General admission         | 3        |
		| CQRS Workshop             | 1        |
		| Additional cocktail party | 2        |
	And the total should read $1197
	And the countdown is started	

Scenario: Checkout:Registrant Details
	Given the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	When the Registrant proceeds to Checkout:Payment
	Then the payment options should be offered for a total of $1197

Scenario: Checkout:Payment and sucessfull Order completed
	Given the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	And the Registrant proceeds to Checkout:Payment
	When the Registrant proceeds to confirm the payment
    Then the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |


Scenario: Allocate all purchased Seats
	Given the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	And the Registrant proceeds to Checkout:Payment
	And the Registrant proceeds to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	When the Registrant assigns these seats
	| seat type                 | first name | last name | email address            |
	| General admission         | William    | Flash     | william@fabrikam.com     |
	| General admission         | Jim        | Corbin    | jim@litwareinc.com       |
	| General admission         | Karen      | Berg      | karen@alpineskihouse.com |
	| CQRS Workshop             | Ryan       | Ihrig     | ryan@cohowinery.com      |
	| Additional cocktail party | Antonio    | Alwan     | antonio@adatum.com       |
	| Additional cocktail party | Jon        | Jaffe     | jon@fabrikam.com         |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
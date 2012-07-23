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

Feature: Registrant workflow for assigning all the purchased Order Items.

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |


Scenario: Allocate all purchased Seats for an individual
	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	And the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	And the Registrant proceeds to Checkout:Payment
	And the Registrant proceeds to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	When the Registrant assigns these seats
	| seat type                 | first name | last name | email address        |
	| General admission         | William    | Flash     | william@fabrikam.com |
	| Additional cocktail party | Jim        | Corbin    | jim@litwareinc.com   |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |


Scenario: Allocate all purchased Seats for a group
	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 4        |
	| Additional cocktail party | 2        |
	And the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address            |
	| William    | Flash     | william@fabrikam.com |
	And the Registrant proceeds to Checkout:Payment
	And the Registrant proceeds to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 4        |
	| Additional cocktail party | 2        |
	When the Registrant assigns these seats
	| seat type                 | first name | last name | email address            |
	| General admission         | William    | Flash     | william@fabrikam.com     |
	| General admission         | Jim        | Corbin    | jim@litwareinc.com       |
	| General admission         | Karen      | Berg      | karen@alpineskihouse.com |
	| General admission         | Ryan       | Ihrig     | ryan@cohowinery.com      |
	| Additional cocktail party | Antonio    | Alwan     | antonio@adatim.com       |
	| Additional cocktail party | Jon        | Jaffe     | jon@fabrika.com          |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 4        |
	| Additional cocktail party | 2        |

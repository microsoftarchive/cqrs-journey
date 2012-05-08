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
	And the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	And the Registrant proceed to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	When the Registrant assign these seats
	| seat type                 | first name | last name | email address            |
	| General admission         | Gregory    | Weber     | gregoryweber@contoso.com |
	| Additional cocktail party | Gregory    | Weber     | gregoryweber@contoso.com |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |


Scenario: Allocate all purchased Seats for a group
	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 4        |
	| Additional cocktail party | 2        |
	And the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| first name | last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	And the Registrant proceed to confirm the payment
    And the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 4        |
	| Additional cocktail party | 2        |
	When the Registrant assign these seats
	| seat type                 | first name | last name | email address       |
	| General admission         | William    | Weber     | William@Weber.com   |
	| General admission         | Gregory    | Doe       | GregoryDoe@live.com |
	| General admission         | Oliver     | Weber     | Oliver@Weber.com    |
	| General admission         | Tim        | Martin    | Tim@Martin.com      |
	| Additional cocktail party | Mani       | Kris      | Mani@Kris.com       |
	| Additional cocktail party | Jim        | Gregory   | Jim@Gregory.com     |
	Then these seats are assigned
	| seat type                 | quantity |
	| General admission         | 4        |
	| Additional cocktail party | 2        |

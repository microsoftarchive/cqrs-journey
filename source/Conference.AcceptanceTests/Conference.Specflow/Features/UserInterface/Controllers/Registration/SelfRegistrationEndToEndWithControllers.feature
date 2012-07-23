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

@SelfRegistrationEndToEndWithControllers
@NoWatiN
Feature: Self Registrant end to end scenario for making a Registration for a Conference site (Implemented using Controllers)
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference, pay for the Registration Order and associate myself with the paid Order automatically

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |


Scenario: End to end Registration implemented using controllers
	Given the Registrant proceeds to make the Reservation
	And these Order Items should be reserved
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	And these Order Items should not be reserved
	| seat type     |
	| CQRS Workshop |
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	And the Registrant proceeds to Checkout:Payment
	When the Registrant proceeds to confirm the payment
	Then the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	And the Registrant assigns these seats
	| seat type                 | first name | last name | email address        |
	| General admission         | William    | Flash     | william@fabrikam.com |
	| Additional cocktail party | Jim        | Corbin    | jim@litwareinc.com   |
	And these seats are assigned
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |

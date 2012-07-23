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

Feature: Self Registrant end to end scenario for making a Registration free of charge for a Conference site
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference free of charge and associate myself with the paid Order automatically

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $0   | 10    |
	| Additional cocktail party | $100 | 10    |

Scenario: Checkout all free of charge
	Given the selected Order Items
	| seat type         | quantity |
	| General admission | 1        |
	And the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	And the total should read $0
	When the Registrant proceeds to Checkout:NoPayment
    Then the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 1        |


Scenario: Checkout partial free of charge
	Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 2        |
	| Additional cocktail party | 3        |
	And the Registrant proceeds to make the Reservation
	And the Registrant enters these details
	| first name | last name | email address        |
	| William    | Flash     | william@fabrikam.com |
	And the total should read $300
	And the Registrant proceeds to Checkout:Payment
	When the Registrant proceeds to confirm the payment
    Then the Registration process was successful
	And the Order should be created with the following Order Items
	| seat type                 | quantity |
	| General admission         | 2        |
	| Additional cocktail party | 3        |
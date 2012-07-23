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

Feature: Promotional Code scenarios for applying Promotional Codes to Seat Types
	In order to apply a Promotional Code for one or more Seat Types
	As a Registrant
	I want to be able to enters a Promotional Code and get the specified price reduction

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate |
	| General admission         | $199 |
	| CQRS Workshop             | $500 |
	| Additional cocktail party | $50  |
	And the Promotional Codes
	| Promotional Code | Discount | Quota     | Scope                            | Cumulative |
	| SPEAKER123       | 100%     | Unlimited | All                              |            |
	| VOLUNTEER        | 100%     | Unlimited | General admission                |            |
	| COPRESENTER      | 10%      | Unlimited | Additional cocktail party        | Exclusive  |
	| WS10             | $20      | Unlimited | All                              | VOLUNTEER  |
	| 1TIMEPRECON      | 50%      | Single    | CQRS Workshop                    |            |
	| CONFONLY         | $50      | Single    | General admission, CQRS Workshop |            |


#Next release
@Ignore
Scenario: Full Promotional Code for all selected items
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $1197
	When the Registrant applies the 'SPEAKER123' Promotional Code
	Then the 'SPEAKER123' Promo code should show a value of -$1197
	And the total amount should be $0


#Next release
@Ignore
Scenario: Partial Promotional Code for all selected items
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $1197
	When the Registrant applies the 'VOLUNTEER' Promotional Code
	Then the 'VOLUNTEER' Promo code should show a value of -$597
	And the total amount should be $600


#Next release
@Ignore
Scenario: Partial Promotional Code for none of the selected items
	Given the selected available Order Items
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $600
	When the Registrant applies the 'VOLUNTEER' Promotional Code
	Then the 'VOLUNTEER' Promo code will not be applied and an error message will inform the Registrant about the problem
	And the total amount should be $600


#Next release
@Ignore
Scenario: Cumulative Promotional Codes
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $1197
	When the Registrant applies the 'COPRESENTER' Promotional Code
	And the Registrant applies the 'WS10' Promotional Code
	Then the 'COPRESENTER' Promotional Code item should show a value of -$10
	And the 'WS10' Promotional Code item should show a value of -$20
	And the total amount should be $1167


#Next release
@Ignore
Scenario: Single use Promotional Code
	Given the selected available Order Items
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $600
	And the Registrant applies the '1TIMEPRECON' Promotional Code
	And the total amount should be of $350
	And the Registrant proceeds to complete the registration
	And the Registrant select the Event Registration
	When the Registrant applies the '1TIMEPRECON' Promotional Code
	Then the '1TIMEPRECON' Promo code will not be applied and an error message will inform the Registrant about the problem
	And the total amount should be $600


#Next release
@Ignore
Scenario: Mutually exclusive Promotional Code
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $1197
	When the Registrant applies the 'COPRESENTER' Promotional Code
	And the Registrant applies the 'VOLUNTEER' Promotional Code
	Then the 'VOLUNTEER' Promo code will not be applied and an error message will inform the Registrant about the problem
	And the 'COPRESENTER' Promotional Code item should show a value of -$10
	And the total amount should be $1187


#Next release
@Ignore
Scenario: Combine only Promotional Code
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 2        |
	And the total amount should be $1197
	When the Registrant applies the 'WS10' Promotional Code
	And the Registrant applies the 'VOLUNTEER' Promotional Code
	Then the 'VOLUNTEER' Promo code should show a value of -$597
	And the 'WS10' Promotional Code item should show a value of -$10
	And the total amount should be $590


#Next release
@Ignore
Scenario: Partial scope
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And the total amount should be $749
	When the Registrant applies the 'CONFONLY' Promotional Code
	Then the 'CONFONLY' Promo code should show a value of -$50
	And the total amount should be $699


# Next release
@Ignore
Scenario: Make a reservation with the selected Order Items and a Promo Code
	Given the Registrant applies the 'COPRESENTER' Promotional Code
	And the 'COPRESENTER' Promo code should show a value of -$5
	When the Registrant proceeds to make the Reservation		
	Then the Reservation is confirmed for all the selected Order Items
	And the total should read $244
	And the countdown is started
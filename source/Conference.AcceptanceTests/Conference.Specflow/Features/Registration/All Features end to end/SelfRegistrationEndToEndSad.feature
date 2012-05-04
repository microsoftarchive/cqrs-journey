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
	Given the list of the available Order Items for the CQRS summit 2012 conference with the slug code SelfRegE2Esad
	| seat type                 | rate | quota |
	| General admission         | $199 | 10    |
	| CQRS Workshop             | $500 | 10    |
	| Additional cocktail party | $50  | 10    |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
#	And the Promotional Codes
#	| Promotional Code | Discount | Quota     | Scope                     | Cumulative |
#	| COPRESENTER      | 10%      | Unlimited | Additional cocktail party | Exclusive  |


Scenario: No selected Seat Type
When the Registrant proceed to make the Reservation with missing or invalid data	
Then the message 'One or more items are required' will show up


#Initial state	: 3 available
#End state		: 2 waitlisted, 1 reserved
Scenario: All Seat Types are available, one get reserved and two get waitlisted
	Given these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                 |
	| CQRS Workshop             |
	| Additional cocktail party |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type                 | quantity |
	| CQRS Workshop             | 1        |
	| Additional cocktail party | 1        |
	And these Order Items should be reserved
	| seat type                        | quantity |
	| General admission                | 1		  |
	And the total should read $199
	And the countdown started


Scenario: Checkout:Registrant Invalid Details
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| First name | Last name | email address        |
	| Gregory    |           | gregoryweber@invalid |
	When the Registrant proceed to Checkout:Payment
	Then the message 'The LastName field is required.' will show up 	


Scenario: Checkout:Payment with cancellation
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| First name | Last name | email address            |
	| Gregory    | Weber     | gregoryweber@contoso.com |
	And the Registrant proceed to Checkout:Payment
	When the Registrant proceed to cancel the payment
    Then the message 'Payment cancelled.' will show up 	


# Next release
@Ignore
Scenario: Partial Promotional Code for none of the selected items
	Given the selected Order Items
	| seat type     | quantity |
	| CQRS Workshop | 1        |
	And the total amount should be of $500
	When the Registrant apply the 'VOLUNTEER' Promotional Code
	Then the 'VOLUNTEER' Promo code will not be applied and an error message will inform about the problem
	And the total amount should be of $500


# Next release
@Ignore
Scenario: Partiall Seats allocation
Given the ConfirmSuccessfulRegistration for the selected Order Items
And the Order Access code is 6789
And I assign the purchased seats to attendees as following
	| First name | Last name | email address            | Seat type         |
	| Gregory    | Weber     | gregoryweber@contoso.com | General admission |
And leave unassigned these seats
	| First name | Last name | email address | Seat type                 |
	|            |           |               | Additional cocktail party |
Then I should be getting a seat assignment confirmation for the seats
	| First name | Last name | email address            | Seat type         |
	| Gregory    | Weber     | gregoryweber@contoso.com | General admission |
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address            | Seat type         |
	| 6789-1      | gregoryweber@contoso.com | General admission |


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

Feature: Registrant workflow for assigning all the registered Order Items.

Background: 
	Given the individual purchased Seats
	| First name | Last name | email address            | Seat type                 |
	| Gregory    | Weber     | gregoryweber@contoso.com | General admission         |
	| Gregory    | Weber     | gregoryweber@contoso.com | Additional cocktail party |
	And the group purchased Seats
	| First name | Last name | email address       | Seat type                 |
	| William    | Weber     | William@Weber.com   | General admission         |
	| Gregory    | Doe       | GregoryDoe@live.com | General admission         |
	| Oliver     | Weber     | Oliver@Weber.com    | CQRS Workshop             |
	| Tim        | Martin    | Tim@Martin.com      | CQRS Workshop             |
	| Mani       | Kris      | Mani@Kris.com       | Additional cocktail party |
	| Jim        | Gregory   | Jim@Gregory.com     | Additional cocktail party |


#Next release
@Ignore
Scenario: Allocate all purchased Seats for an individual
Given the ConfirmSuccessfulRegistration for the selected Order Items
And the Order Access code is 6789
And the Registrant assign the infividual purchased Seats to attendees as following
	| First name | Last name | email address            | Seat type                 |
	| Gregory    | Weber     | gregoryweber@contoso.com | General admission         |
	| Gregory    | Weber     | gregoryweber@contoso.com | Additional cocktail party |
Then the Registrant should get a Seat assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address            | Seat type                 |
	| 6789-1      | gregoryweber@contoso.com | General admission         |
	| 6789-2      | gregoryweber@contoso.com | Additional cocktail party |

	
#Next release
@Ignore
Scenario: Allocate all purchased Seats for a group
Given the ConfirmSuccessfulRegistration
And the order access code is 6789
And the Registrant assign the group purchased Seats to attendees as following
	| First name | Last name | email address       | Seat type                 |
	| William    | Weber     | William@Weber.com   | General admission         |
	| Gregory    | Doe       | GregoryDoe@live.com | General admission         |
	| Oliver     | Weber     | Oliver@Weber.com    | CQRS Workshop             |
	| Tim        | Martin    | Tim@Martin.com      | CQRS Workshop             |
	| Mani       | Kris      | Mani@Kris.com       | Additional cocktail party |
	| Jim        | Gregory   | Jim@Gregory.com     | Additional cocktail party |
Then the Registrant should get a Seat Assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address       | Seat type                 |
	| 6789-1      | William@Weber.com   | General admission         |
	| 6789-2      | GregoryDoe@live.com | General admission         |
	| 6789-3      | Oliver@Weber.com    | CQRS Workshop             |
	| 6789-4      | Tim@Martin.com      | CQRS Workshop             |
	| 6789-5      | Mani@Kris.com       | Additional cocktail party |
	| 6789-6      | Jim@Gregory.com     | Additional cocktail party |

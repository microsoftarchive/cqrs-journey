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

@ConferenceConfigurationIntegration
@NoWatiN
Feature:  Conference configuration scenarios for creating and editing Conference settings with events and commands
	In order to create or update a Conference configuration
	As a Business Customer
	I want to be able to create or update a Conference and set its properties

Background: 
Given this conference information
	| Owner         | Email                | Name      | Description                             | Slug   | Start      | End        |
	| William Flash | william@fabrikam.com | CQRS2012P | CQRS summit 2012 conference (Published) | random | 05/02/2012 | 05/12/2012 |

Scenario: A new Conference is created and published
	When the conference is created
	And the conference is published
	#ConferenceCreated
	Then the event for publishing the conference is emitted
	#ConferencePublished
	And the event for publishing the conference is emitted

Scenario: Adding Seats to an existing conference
	Given the conference already exists
	And the conference is published
	When these Seat Types are created
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
	#SeatCreated 
	Then the events for creating the Seat Type are emitted

﻿# ==============================================================================================================
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

Feature:  Conference configuration scenarios for creating and editing Conference settings with events and commands
	In order to create or update a Conference configuration
	As a Business Customer
	I want to be able to create or update a Conference and set its properties

Scenario: An existing unpublished Conference is selected and published
Given this conference information
| Owner         | Email                    | Name      | Description                             | Slug   | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012P | CQRS summit 2012 conference (Published) | random | 05/02/2012 | 05/12/2012 |
#ConferenceCreated
When the event for creating the conference is emitted
#ConferencePublished
And the event for publishing the conference is emitted
Then the conference is created and published


Scenario: Adding Seats to an existing conferences
Given this conference information
| Owner         | Email                    | Name      | Description                             | Slug   | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012P | CQRS summit 2012 conference (Published) | random | 05/02/2012 | 05/12/2012 |
And the conference already exists
When these Seat Types are created
| Name   | Description       | Quantity | Price |
| GENADM | General admission | 100      | 199   |
#SeatCreated 
Then the event for creating a Seat is emitted 

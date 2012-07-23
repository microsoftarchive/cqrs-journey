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

Feature:  Conference configuration scenarios for creating and editing Conference settings
	In order to create or update a Conference configuration
	As a Business Customer
	I want to be able to create or update a Conference and set its properties


Background: 
Given the Business Customer selected the Create Conference option

Scenario: A new Conference is created with the required information
Given this conference information
| Owner         | Email                | Name      | Description                          | Slug   | Start      | End        |
| William Flash | william@fabrikam.com | CQRS2012P | CQRS summit 2012 conference (Create) | random | 05/02/2012 | 05/12/2012 |
When the Business Customer proceeds to create the Conference
Then following details will be shown for the created Conference
| Owner         | Email                | AccessCode |
| William Flash | william@fabrikam.com | random     |


Scenario: An existing unpublished Conference is selected and published
Given this conference information
| Owner         | Email                | Name      | Description                             | Slug   | Start      | End        |
| William Flash | william@fabrikam.com | CQRS2012P | CQRS summit 2012 conference (Published) | random | 05/02/2012 | 05/12/2012 |
And the Business Customer proceeds to create the Conference
When the Business Customer proceeds to publish the Conference
Then the state of the Conference changes to Published


Scenario: An existing Conference is edited and updated
Given an existing published conference with this information
| Owner         | Email                | Name      | Description                            | Slug   | Start      | End        |
| William Flash | william@fabrikam.com | CQRS2012U | CQRS summit 2012 conference (Original) | random | 05/02/2012 | 05/12/2012 |
And the Business Customer proceeds to edit the existing settings with this information
| Description                           |
| CQRS summit 2012 conference (Updated) |
When the Business Customer proceeds to save the changes
Then this information is shown in the Conference settings
| Description                           |
| CQRS summit 2012 conference (Updated) |


Scenario: Access an existing Conference
Given an existing published conference with this information
| Owner         | Email                | Name      | Description                          | Slug   | Start      | End        |
| William Flash | william@fabrikam.com | CQRS2012A | CQRS summit 2012 conference (Access) | random | 05/02/2012 | 05/12/2012 |
When the Business Customer proceeds to get access to the conference settings
Then this information is shown in the Conference settings
| Owner         | Email                | Name      | Description                          | Slug   | Start      | End        |
| William Flash | william@fabrikam.com | CQRS2012A | CQRS summit 2012 conference (Access) | random | 05/02/2012 | 05/12/2012 |




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

Feature:  Conference configuration scenarios for creating and editing Conference settings
	In order to create or update a Conference configuration
	As a Business Customer
	I want to be able to create or update a Conference and set its properties


Background: 
Given the Business Customer selected the Create Conference option

@Ignore
Scenario: A new Conference is created with the required information
Given this information entered into the Owner Information section
| Owner         | Email                    |
| Gregory Weber | gregoryweber@contoso.com |
And this inforamtion entered into the Conference Information section
| Name     | Description                 | Slug     | Start      | End        |
| CQRS2012 | CQRS summit 2012 conference | cqrs2012 | 05/02/2012 | 05/12/2012 |
When the Business Customer proceed to create the Conference
Then following details will be shown for the created Conference
| Owner         | Email                    | Name     | Description                 | Slug     | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012 | CQRS summit 2012 conference | cqrs2012 | 05/02/2012 | 05/12/2012 |


@Ignore
Scenario: An existing unpublished Conference is selected and published
Given an existing conference with this information
| Owner         | Email                    | Name      | Description                             | Slug      | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012P | CQRS summit 2012 conference (Published) | cqrs2012p | 05/02/2012 | 05/12/2012 |
When the Business Customer proceed to publish the Conference
Then the state of the Conference change to Published


@Ignore
Scenario: An existing Conference is edited and updated
Given an existing conference with this information
| Owner         | Email                    | Name      | Description                            | Slug      | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012U | CQRS summit 2012 conference (Original) | cqrs2012p | 05/02/2012 | 05/12/2012 |
And the Business Customer proceed to update the existing settigns with this information
| Description                           |
| CQRS summit 2012 conference (Updated) |
When the Business Customer proceed to save the changes
Then this information is show up in the Conference settings
| Description                           |
| CQRS summit 2012 conference (Updated) |


@Ignore
Scenario: Access an existing Conference
Given an existing conference with this information
| Owner         | Email                    | Name      | Description                          | Slug      | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012A | CQRS summit 2012 conference (Access) | cqrs2012p | 05/02/2012 | 05/12/2012 |
When the Business Customer proceed to get acecss to the conference settings
Then this information is show up in the Conference settings
| Owner         | Email                    | Name      | Description                          | Slug      | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012A | CQRS summit 2012 conference (Access) | cqrs2012p | 05/02/2012 | 05/12/2012 |




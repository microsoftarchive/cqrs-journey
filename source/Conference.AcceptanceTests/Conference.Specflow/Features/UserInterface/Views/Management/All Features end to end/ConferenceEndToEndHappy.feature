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

Feature: End to end successful Conference configuration scenarios for creating and editing Conference settings 
	In order to create or update a Conference configuration
	As a Business Customer
	I want to be able to create or update a Conference and set its properties


Background: 
Given the Business Customer selected the Create Conference option

Scenario: A new Conference is created with the required information
Given this conference information
| Owner         | Email                    | Name      | Description                          | Slug   | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012P | CQRS summit 2012 conference (Create) | random | 05/02/2012 | 05/12/2012 |
When the Business Customer proceed to create the Conference
Then following details will be shown for the created Conference
| Owner         | Email                    | AccessCode |
| Gregory Weber | gregoryweber@contoso.com | random     |


@Ignore
Scenario: Seat Types are created and assigned to an existing Conference
Given an existing unpublished conference with this information
| Owner         | Email                    | Name      | Description                                   | Slug      | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012S | CQRS summit 2012 conference (Seat Assignment) | cqrs2012p | 05/02/2012 | 05/12/2012 |
And the information for the new Seat Types
| Name   | Description       | Quantity | Price |
| GENADM | General admission | 100      | 199   | 
When the Business Customer proceed to create the Seat Types
Then the new Seat Types with this information are created
| Name   | Description       | Quantity | Price  |
| GENADM | General admission | 100      | 199,00 |


@Ignore
Scenario: Create a new Promotional Code
Given the Promotional Codes
    | Promotional Code | Discount | Quota     | Scope | Cumulative |
    | SPEAKER123       | 100%     | Unlimited | All   |            |
And the Seat Types configuration
	| seat type                 | quota |
	| General admission         | 500   |
	| CQRS Workshop             | 100   |
	| Additional cocktail party | 600   |
And the Business Customer proceed to create a Promotional Code
And the Business Customer enter the 'NEWCODE' Promotional Code and these attributes
	| Discount | Quota     | Scope             | Cumulative |
	| 10%      | Unlimited | General admission | SPEAKER123 |
When the Business Customer proceed to save the new information
Then the new Promotional Code is added to the list of existing codes
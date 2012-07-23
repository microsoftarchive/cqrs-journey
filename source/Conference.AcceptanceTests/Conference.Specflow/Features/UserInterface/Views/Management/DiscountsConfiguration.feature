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

Feature:  Promotional Codes configuration scenarios for creating and editing Promotional Codes settings
	In order to create or update a Promotional Code
	As a Business Customer
	I want to be able to create or update a Promotional Code and set its properties

Background: 
Given the Promotional Codes
         | Promotional Code | Discount | Quota     | Scope | Cumulative |
         | SPEAKER123       | 100%     | Unlimited | All   |            |
And the Seat Types configuration
	| seat type                 | quota |
	| General admission         | 500   |
	| CQRS Workshop             | 100   |
	| Additional cocktail party | 600   |

# New Promo Code  Happy path
#Next release
@Ignore
Scenario: Create a new Promotional Code
Given the Business Customer proceeds to create a Promotional Code
And the Business Customer enters the 'NEWCODE' Promotional Code and these attributes
	| Discount | Quota     | Scope             | Cumulative |
	| 10%      | Unlimited | General admission | SPEAKER123 |
When the Business Customer proceeds to save the new information
Then the new Promotional Code is added to the list of existing codes


# New Promo Code Sad path
#Next release
@Ignore
Scenario: Create a new Promotional Code with a quota that exceeds the available seats
Given the Business Customer proceeds to create a Promotional Code
And the Business Customer enters the 'NEWCODE' Promotional Code and these attributes
	| Discount | Quota | Scope             | Cumulative |
	| 10%      | 1000  | General admission | SPEAKER123 |
When the Business Customer proceeds to save the new information
Then an error message will show up describing that the quota value exceeds the total seats for the specified Seat Type


# Update Promo Code Happy path
#Next release
@Ignore
Scenario: Update an existing Promotional Code
Given the Business Customer select 'SPEAKER123' Promotional Code
And the Scope is updated with value 'CQRS Workshop'
And the Quota is updated with the value '50'
When the Business Customer proceeds to save the new information
Then updated values are reflected in the selected Promotional Code


# Update Promo Code Sad path
#Next release
@Ignore
Scenario: Update an existing Promotional Code with a quota that exceeds the available seats
Given the Business Customer select 'SPEAKER123' Promotional Code
And the Scope is updated with value 'CQRS Workshop'
And the Quota is updated with the value '200'
When the Business Customer proceeds to save the new information
Then an error message will show up describing that the quota value exceeds the total seats for the specified Seat Type





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

Feature:  Seat Types configuration scenarios for creating and editing Seat Types settings
	In order to create or update a Seat Type
	As a Business Customer
	I want to be able to create or update a Seat Type and set its properties

Background: 
Given an existing unpublished conference with this information
| Owner         | Email                    | Name      | Description                                   | Slug   | Start      | End        |
| Gregory Weber | gregoryweber@contoso.com | CQRS2012S | CQRS summit 2012 conference (Seat Assignment) | random | 05/02/2012 | 05/12/2012 |
And the Business Customer selects the Seat Types opcion

@Ignore
Scenario: Seat Types are created and assigned to an existing Conference
Given the Business Customer proceed to create new Seat Types
And the information for the Seat Types
| Name   | Description       | Quantity | Price |
| GENADM | General admission | 100      | 199   | 
When the Business Customer proceed to create the Seat Types
Then the new Seat Types with this information are created
| Name   | Description       | Quantity | Price  |
| GENADM | General admission | 100      | 199,00 |

@Ignore
Scenario: Seat Types are edited from existing Conference
Given the existing Seat Types
| Name   | Description       | Quantity | Price |
| GENADM | General admission | 100      | 199   | 
And the updated information
| Quantity |
| 200      |
When the Business Customer proceed to save the changes
Then the updated Seat Type will show up
| Quantity |
| 200      |

@Ignore
Scenario: Seat Types are deleted from existing Conference
Given the existing Seat Types
| Name   | Description       | Quantity | Price |
| GENADM | General admission | 100      | 199   | 
When the Business Customer proceed to delete the selected Seat Type
Then the Seat Type is removed from the Conference
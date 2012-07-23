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

Feature:  Multiple Conference configuration scenarios for creating and editing many Conferences
	In order to create multiple Conferences
	As a Business Customer
	I want to be able to create multiple Conferences and set their properties

#Multiple creation scenario
#The %1 placeholder will be replaced with an ordinal counter.
#Future regression research
@Ignore 
Scenario: Multiple Seat Types are created and assigned to a new existing Conference
Given this base conference information
| Owner   | Email           | Name    | Description              | Slug    | Start      | End        |
| Neuro%1 | neuro@fabrikam.com | NEURO%1 | Neuro Test conference %1 | neuro%1 | 05/02/2012 | 07/12/2012 |
And these Seat Types
| Name  | Description      | Quantity | Price |
| TEST1 | Test seat type 1 | 100000   | 0     |
| TEST2 | Test seat type 2 | 100000   | 1     | 
When the Business Customer proceeds to create 3 'random' conferences
Then all the conferences are created

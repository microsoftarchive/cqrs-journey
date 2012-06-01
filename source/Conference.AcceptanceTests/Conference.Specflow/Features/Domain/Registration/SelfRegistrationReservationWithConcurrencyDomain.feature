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

@NoWatiN
Feature: Self-Registrant scenarios for making a Reservation for a Conference site where multiple Registrants make simultaneous reservations
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available where other Registrants may also be interested

#This scenario with low volume is for Azure build  
@SelfRegistrationReservationWithConcurrencyDomain
Scenario: Some Registrants try to reserve the same Order Item and not all of them get the reservation	
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type     | rate | quota |
	| CQRS Workshop | $500 | 4     |
	When 4 Registrants selects these Order Items
	| seat type     | quantity |
	| CQRS Workshop | 2        |
	# event: OrderReservationCompleted 
	Then only 2 events for completing the Order reservation are emitted
	# event: OrderPartiallyReserved 
	And 2 events for partially completing the order are emitted


#This scenario with hight volume is for Sql build (DebugLocal)  
#Self Registrant scenario
@SelfRegistrationReservationWithConcurrencyDomainDebugLocalOnly
Scenario: Many Registrants try to reserve the same Order Item and not all of them get the reservation	
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type     | rate | quota |
	| CQRS Workshop | $500 | 200   |
	When 200 Registrants selects these Order Items
	| seat type     | quantity |
	| CQRS Workshop | 2        |
	# event: OrderReservationCompleted 
	Then only 100 events for completing the Order reservation are emitted
	# event: OrderPartiallyReserved 
	And 100 events for partially completing the order are emitted


#This scenario with hight volume is for Sql build (DebugLocal)  
#Group Registrant scenario with some partial and some full reservations
@SelfRegistrationReservationWithConcurrencyDomainDebugLocalOnly
Scenario: Many Registrants try to reserve many same Order Items and some of them get a partial reservation
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type         | rate | quota |
	| CQRS Workshop     | $500 | 200   |
	| General admission | $199 | 100   |
	When 200 Registrants selects these Order Items
	| seat type         | quantity |
	| CQRS Workshop     | 1        |
	| General admission | 2        |
	# event: OrderReservationCompleted 
	Then only 50 events for completing the Order reservation are emitted
	# event: OrderPartiallyReserved 
	And 150 events for partially completing the order are emitted
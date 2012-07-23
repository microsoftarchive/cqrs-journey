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

Feature: Self-Registrant scenarios for making a Reservation for a Conference site where two Registrants make simultaneous reservations
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available where other Registrants may also be interested at the same time

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type     | rate | quota |
	| CQRS Workshop | $500 | 10    |

#Future regression research
@Ignore  
Scenario: Only one Order Item is available and two Registrants try to reserve it, then only one gets the reservation	
	Given the selected Order Items
	| seat type     | quantity |
	| CQRS Workshop | 6        |
	And another Registrant select these Order Items 
	| seat type     | quantity |
	| CQRS Workshop | 6        |
	When the Registrant proceeds to make the Reservation
	And another Registrant proceeds to make the Reservation with seats already reserved 
	Then the Reservation is confirmed for all the selected Order Items
	And a second Reservation is offered to select any of these available seats
	| seat type     | selected | message                                    |
	| CQRS Workshop | 4        | Could not reserve all the requested seats. |


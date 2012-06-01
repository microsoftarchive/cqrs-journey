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

@RegistrationProcessHardeningWithDomain
@NoWatiN
Feature: The RegistrationProcess should be able to recover from unexpected conditions and failures
	There are two general issues to consider
	Messages are handled successfully but they cannot be completed so they are handled again and
	the process state is stored but the commands it generates fail to be published

#The RegistrationProcess should be able to recover from the following:
#- Crashes or is unable to access storage after receiving an event and before it sends any commands.
#- Crashes or is unable to access storage after saving its own state but before sending the commands.
#- Fails to mark an event as processed after it has finished doing all of its work.
#- Times-out because it is expecting a timely response from any of the commands it has sent, but for some reason the recipients of those commands fail to respond.
#- Receives an unexpected event (i.e. PaymentCompleted after the order has been expired).


Scenario: Crashes or is unable to access storage after receiving an event and before it sends any commands
Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 100   |
	| CQRS Workshop             | $500 | 100   |
	| Additional cocktail party | $50  | 100   |
And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
When the Registrant proceed to make the Reservation
	# command:RegisterToConference
Then the command to register the selected Order Items is received 
	# event: OrderPlaced
And the event for Order placed is emitted
	# command: MakeSeatReservation
And the command for reserving the selected Seats is received
	# event: SeatsReserved
And the event for reserving the selected Seats is emitted
	# command: MarkSeatsAsReserved
And the command for marking the selected Seats as reserved is received
	# event: OrderReservationCompleted 
And the event for completing the Order reservation is emitted
	# event: OrderTotalsCalculated
And the event for calculating the total of $249 is emitted
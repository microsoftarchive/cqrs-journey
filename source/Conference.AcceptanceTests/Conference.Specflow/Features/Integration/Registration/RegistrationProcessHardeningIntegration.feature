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

@RegistrationProcessHardeningIntegration
@NoWatiN
Feature: Hardening the RegistrationProcess so it is able to recover from unexpected conditions and failures
	There are two general issues to consider
	Messages are handled successfully but they cannot be completed so they are handled again and
	the process state is stored but the commands it generates fail to be published

#The RegistrationProcess should be able to recover from the following:
#- Crashes or is unable to access storage after saving its own state but before sending the commands.
#- Crashes or is unable to access storage after receiving an event and before it sends any commands.
#- Fails to mark an event as processed after it has finished doing all of its work.
#- Times-out because it is expecting a timely response from any of the commands it has sent, but for some reason the recipients of those commands fail to respond.
#- Receives an unexpected event (i.e. PaymentCompleted after the order has been expired).

Background: 
Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                 | rate | quota |
	| General admission         | $199 | 20    |
	| CQRS Workshop             | $500 | 20    |
	| Additional cocktail party | $50  | 20    |
And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |


#- Crashes or is unable to access storage after receiving an event and before it sends any commands.
Scenario: The Command to Register the order is lost after a crash
When the command to register the selected Order Items is lost 
And the event for Order placed is emitted
	# command: MakeSeatReservation
Then the command for reserving the selected Seats is received


#- Times-out because it is expecting a timely response from any of the commands it has sent, but for some reason the recipients of those commands fail to respond.
Scenario: Times-out after the reservation is completed
	# event: OrderPlaced
When the event for Order placed gets expired
Then the event for confirming the Order is not emitted


#- Receives an unexpected event (i.e. PaymentCompleted after the order has been expired).
Scenario: Execute the Payment process after the order has expired
Given the event for Order placed gets expired
	# command: InitiateThirdPartyProcessorPayment
When the command for initiate the payment is sent
	# command: CompleteThirdPartyProcessorPayment
And the command for completing the payment process is sent
	# event: PaymentCompleted
Then the event for confirming the payment is emitted
	# command: OrderConfirmed 
And the event for confirming the Order is not emitted



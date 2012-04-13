Feature: Order Items Reservation scenarios to verify the countdown management and metrics.
	In order to reserve selected Order Items, they will be held for an arbitary amount of time 
	so the Registrant may complete the Order for the selected Order Items

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                        | rate |
	| General admission                | $199 |
	| Pre-con Workshop with Greg Young | $500 |
	| Additional cocktail party		   | $50  |	
	And the selected Order Items
	| seat type                        | quantity |
	| General admission                | 1        |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 1        |


Scenario: Make a reservation with the selected Order Items
	When the Registrant proceed to make the Reservation	for the selected Order Items		
	Then the Reservation is confirmed for all the selected Order Items
	And the total should read $249
	And the countdown started for these Order Items
	| seat type                        | quantity |
	| General admission                | 1        |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 1        |


#Initial state	: 3 available
#End state		: 2 waitlisted, 1 reserved
Scenario: All Seat Types are available, one get reserved and two get waitlisted
	Given these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                        |
	| Pre-con Workshop with Greg Young |
	| Additional cocktail party		   |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 1		  |
	And These other Order Items get reserved
	| seat type                        | quantity |
	| General admission                | 1		  |
	And the countdown started for these Order Items
	| seat type                 | quantity |
	| General admission         | 1        |

#Previous scenario but with timeout
Scenario: The Registrant exceed the allowed time when offered to be waitlisted and has 1 Order Items reserved
	Given the Registrant is offered to be waitlisted for these Order Items
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 1		  |
	And These other Order Items get reserved
	| seat type                        | quantity |
	| General admission                | 1		  |
	And the countdown started for these Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	When the time to confirm the reservation has exceeded the countdown		
	Then the Reservation is cancelled and a message should be shown regarding the expired countdown
	And the Registrant should be offered to start over the Reservation with the available seats
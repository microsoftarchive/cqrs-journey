Feature: Self Registrant scenarios for making a Reservation for a Conference site with all Order Items initially available
	In order to reserve Seats for a conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items selected for the CQRS summit 2012 conference
	| seat type                        | quantity |
	| General admission                | 1		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 1		  |	


#1
#Initial state	: 3 available
#End state		: 3 reserved	
Scenario: All the Order Items are available and all get reserved
	When the Registrant proceed to make the Reservation			
	Then the Reservation is confirmed for all the selected Order Items.


#2
#Initial state	: 3 available
#End state		: 3 waitlisted
Scenario: All the Order Items are available and all get waitlisted
	Given these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type                        |
	| General admission                |
	| Pre-con Workshop with Greg Young |
	| Additional cocktail party		   |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type                        | quantity |
	| General admission                | 1		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 1		  |
	And these waitlist registration details will be asked
	| waitlist registration details |
	| First Name                    |
	| Last Name                     |
	| Email Address                 |


#3
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

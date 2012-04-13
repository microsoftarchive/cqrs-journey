Feature: Registrant scenarios for registering a group of Attendees for a conference when few Seats are available in all the Seat Types
	In order to register for conference a group of Attendees
	As a Registrant
    I want to be able to select Order Items from one or many of the available and or waitlisted Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                        | rate |
	| General admission                | $199 |
	| Pre-con Workshop with Greg Young | $500 |
	| Additional cocktail party		   | $50  |	

#1
#Initial state	: 3 waitlisted and 3 selected
#End state		: 3 waitlisted confirmed  
Scenario: All the Order Items are offered to be waitlisted and all are selected, then all get confirmed	
	Given the list of Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| General admission                | 3		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 2		  |
	When the Registrant proceed to make the Reservation			
	Then these Order Itmes get confirmed being waitlisted
	| seat type                        | quantity |
	| General admission                | 3		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 2		  |	

#2
#Initial state	: 2 available items and 1 waitlisted, 3 selected
#End state		: 2 reserved, 2 waitlisted
Scenario: 2 the Order Items are available and 1 waitlisted, 1 becomes partially available,
	      then 2 are partially offered to get waitlisted and 2 get reserved
	Given the selected available Order Items
	| seat type                        | quantity |
	| General admission                | 7        |
	| Pre-con Workshop with Greg Young | 2        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| Additional cocktail party        | 5        |	
	And these Seat Types becomes partially unavailable before the Registrant make the reservation
	| seat type         |
	| General admission |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type                 | quantity |
	| General admission         | 3        |
	| Additional cocktail party | 5        |
	And These other Order Items get reserved
	| seat type                        | quantity |
	| General admission                | 4        |
	| Pre-con Workshop with Greg Young | 2        |
	And the total amount should be of $1796











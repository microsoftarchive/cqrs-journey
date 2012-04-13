Feature: Registrant workflow for registering a group of Attendees for a conference (sad path)
	In order to register for conference a group of Attendees
	As a Registrant
	I want to be able to select Order Items from one or many available Order Items and make a Reservation

#General preconditions for all the scenarios
Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                        | rate |
	| General admission                | $199 |
	| Pre-con Workshop with Greg Young | $500 |
	| Additional cocktail party		   | $50  |	


#Initial state	: 3 available items, 2 selected
#End state		: 1 reserved and 1 offered waitlisted
Scenario: All the Order Items are available, then some get waitlisted and some reserved
	Given the selected available Order Items
	| seat type                 | quantity |
	| General admission         | 2        |
	| Additional cocktail party | 2        |
	And these Seat Types becomes unavailable before the Registrant make the reservation
	| seat type         |
	| General admission |
	When the Registrant proceed to make the Reservation			
	Then the Registrant is offered to be waitlisted for these Order Items
	| seat type         | quantity |
	| General admission | 1        |
	And These other Order Items get reserved
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 2        |


Scenario: Checkout:Registrant Invalid Details
	Given the Registrant enter these details
	| First name | Last name | email address     | Seat type                 |
	| William    |           | William@Smith.com | General admission         |
	| Mani       | Kris      | Mani@Kris.com     | Additional cocktail party |
	| Jim        | John      | Jim@John.com      | Additional cocktail party |
	And the Last name is empty
	# valid = non-empty, email address is valid as per email conventional verification
	When the Registrant proceed to select a payment option
	Then the invalid field is highlighted with a hint of the error cause
	And the countdown has decreased within the allowed timeslot for holding the Reservation


Scenario: Checkout:Payment with cancellation
	Given the Registrant enter these details
	| First name | Last name | email address     | Seat type                 |
	| William    | Smith     | William@Smith.com | General admission         |
	| Mani       | Kris      | Mani@Kris.com     | Additional cocktail party |
	| Jim        | John      | Jim@John.com      | Additional cocktail party |
	And the countdown has decreased within the allowed timeslot for holding the Reservation
	And the Registrant select one of the offered payment options
	When the Registrant decides to cancel the payment
    Then a cancelation message will be shown to the Registrant and will get back to the payment options

	
Scenario: Checkout:Partial Payment and place Order
	Given the Registrant enter these details
	| Mani    | Kris  | Mani@Kris.com     | Additional cocktail party |
	| William | Smith | William@Smith.com | General admission         |
	| Mani    | Kris  | Mani@Kris.com     | Additional cocktail party |
	And the countdown has decreased within the allowed timeslot for holding the Reservation
	And the Registrant select one of the offered payment options
	When the Registrant proceed to confirm the payment
    Then a receipt will be received from the payment provider indicating success with some transaction id
	And a Registration confirmation with the Access code should be displayed
	And an email with the Access Code will be send to the registered email. 


Scenario: Allocate some purchased Seats for a group
Given the ConfirmSuccessfulRegistration
And the order access code is 6789
And the Registrant assign the group purchased Seats to attendees as following
	| First name | Last name | email address     | Seat type         |
	| William    | Smith     | William@Smith.com | General admission |
And leave unassigned these individual purchased seats
	| First name | Last name | email address | Seat type                 |
	| Mani       | Kris      | Mani@Kris.com | Additional cocktail party |
Then the Registrant should get a Seat Assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address     | Seat type                        |
	| 6789-1      | William@Smith.com | General admission                |






Feature: Registrant workflow for registering a group of Attendees for a conference (happy path)
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


#1
#Initial state	: 3 available items, 3 selected
#End state		: 3 reserved	
Scenario: All the Order Items are available and all get selected, then all get reserved
	Given the selected available Order Items
	| seat type                        | quantity |
	| General admission                | 3        |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 2        |
	When the Registrant proceed to make the Reservation
	Then the Reservation is confirmed for all the selected Order Items.
	And the total amount should be of $1197
	

Scenario: Checkout:Registrant Details
	Given the Registrant enter these details
	| First name | Last name | email address     | Seat type                        |
	| William    | Smith     | William@Smith.com | General admission                |
	| John       | Doe       | JohnDoe@live.com  | General admission                |
	| Oliver     | Smith     | Oliver@Smith.com  | Pre-con Workshop with Greg Young |
	| Tim        | Martin    | Tim@Martin.com    | Pre-con Workshop with Greg Young |
	| Mani       | Kris      | Mani@Kris.com     | Additional cocktail party        |
	| Jim        | John      | Jim@John.com      | Additional cocktail party        |
	And the Registrant details are valid
	# valid = non-empty, email address is valid as per email conventional verification
	When the Registrant proceed to Checkout:Payment
	Then the payment options shoule be offered
	And the countdown has decreased within the allowed timeslot for holding the Reservation

Scenario: Checkout:Payment and sucessfull Order completed
	Given Checkout:Registrant Details completed
	And the countdown has decreased within the allowed timeslot for holding the Reservation
	And the Registrant select one of the offered payment options
	When the Registrant proceed to confirm the payment
    Then a receipt will be received from the payment provider indicating success with some transaction id
	And a Registration confirmation with the Access code should be displayed
	And an email with the Access Code will be send to the registered email. 


Scenario: Allocate all purchased Seats for a group
Given the ConfirmSuccessfulRegistration
And the order access code is 6789
And the Registrant assign the group purchased Seats to attendees as following
	| First name | Last name | email address     | Seat type                        |
	| William    | Smith     | William@Smith.com | General admission                |
	| John       | Doe       | JohnDoe@live.com  | General admission                |
	| Oliver     | Smith     | Oliver@Smith.com  | Pre-con Workshop with Greg Young |
	| Tim        | Martin    | Tim@Martin.com    | Pre-con Workshop with Greg Young |
	| Mani       | Kris      | Mani@Kris.com     | Additional cocktail party        |
	| Jim        | John      | Jim@John.com      | Additional cocktail party        |
Then the Registrant should get a Seat Assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address     | Seat type                        |
	| 6789-1      | William@Smith.com | General admission                |
	| 6789-2      | JohnDoe@live.com  | General admission                |
	| 6789-3      | Oliver@Smith.com  | Pre-con Workshop with Greg Young |
	| 6789-4      | Tim@Martin.com    | Pre-con Workshop with Greg Young |
	| 6789-5      | Mani@Kris.com     | Additional cocktail party        |
	| 6789-6      | Jim@John.com      | Additional cocktail party        |
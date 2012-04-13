Feature: Self Registrant end to end scenario for making a Registration for a Conference site (happy path)
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference, pay for the Registration Order and associate myself with the paid Order automatically

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference
	| seat type                        | rate |
	| General admission                | $199 |
	| Pre-con Workshop with Greg Young | $500 |
	| Additional cocktail party		   | $50  |	
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
	And the Promotional Codes
	| Promotional Code | Discount | Quota     | Scope                     | Cumulative |
	| COPRESENTER      | 10%      | Unlimited | Additional cocktail party | Exclusive  |

Scenario: Make a reservation with the selected Order Items
	Given the Registrant apply the 'COPRESENTER' Promotional Code
	And the 'COPRESENTER' Coupon item should show a value of -$5
	When the Registrant proceed to make the Reservation	for the selected Order Items		
	Then the Reservation is confirmed for all the selected Order Items
	And the total should read $244
	And the countdown started


Scenario: Checkout:Registrant Details
	Given the Registrant enter these details
	| First name | Last name | email address         |
	| John       | Smith     | johnsmith@contoso.com |
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


Scenario: AllocateSeats
Given the ConfirmSuccessfulRegistration for the selected Order Items
And the Order Access code is 6789
And the Registrant assign the purchased seats to attendees as following
	| First name | Last name | email address         | Seat type                 |
	| John       | Smith     | johnsmith@contoso.com | General admission         |
	| John       | Smith     | johnsmith@contoso.com | Additional cocktail party |
Then the Regsitrant should be get a Seat Assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address         | Seat type                 |
	| 6789-1      | johnsmith@contoso.com | General admission         |
	| 6789-2      | johnsmith@contoso.com | Additional cocktail party |

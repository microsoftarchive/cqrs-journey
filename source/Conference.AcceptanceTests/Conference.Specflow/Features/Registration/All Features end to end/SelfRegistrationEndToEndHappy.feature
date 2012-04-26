Feature: Self Registrant end to end scenario for making a Registration for a Conference site (happy path)
	In order to register for a conference
	As an Attendee
	I want to be able to register for the conference, pay for the Registration Order and associate myself with the paid Order automatically

Background: 
	Given the list of the available Order Items for the CQRS summit 2012 conference SelfRegE2Ehappy
	| seat type                        | rate | quota |
	| General admission                | $199 | 100   |
	| Pre-con Workshop with Greg Young | $500 | 100   |
	| Additional cocktail party        | $50  | 100   |
	And the selected Order Items
	| seat type                 | quantity |
	| General admission         | 1        |
	| Additional cocktail party | 1        |
#	And the Promotional Codes
#	| Promotional Code | Discount | Quota     | Scope                     | Cumulative |
#	| COPRESENTER      | 10%      | Unlimited | Additional cocktail party | Exclusive  |


Scenario: Make a reservation with the selected Order Items
	When the Registrant proceed to make the Reservation		
	Then the Reservation is confirmed for all the selected Order Items
	And these Order Items should be reserved
		| seat type                 |
		| General admission         |
		| Additional cocktail party |
	And these Order Items should not be reserved
		| seat type                        |
		| Pre-con Workshop with Greg Young |
	And the total should read $249
	And the countdown started

#Promo code not implemented yet
@ignore
Scenario: Make a reservation with the selected Order Items and a Promo Code
	Given the Registrant apply the 'COPRESENTER' Promotional Code
	And the 'COPRESENTER' Promo code should show a value of -$5
	When the Registrant proceed to make the Reservation		
	Then the Reservation is confirmed for all the selected Order Items
	And the total should read $244
	And the countdown started

Scenario: Checkout:Registrant Details
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| First name | Last name | email address         |
	| John       | Smith     | johnsmith@contoso.com |
	When the Registrant proceed to Checkout:Payment
	Then the payment options should be offered for a total of $249

Scenario: Checkout:Payment and sucessfull Order completed
	Given the Registrant proceed to make the Reservation
	And the Registrant enter these details
	| First name | Last name | email address         |
	| John       | Smith     | johnsmith@contoso.com |
	And the Registrant proceed to Checkout:Payment
	When the Registrant proceed to confirm the payment
    Then the message 'You will receive a confirmation e-mail in a few minutes.' will show up
	And the Order should be located from the Find Order page 

#Seat allocation not implemented yet
@ignore
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

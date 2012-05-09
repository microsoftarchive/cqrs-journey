Feature: Discounts
	In order to save money
	As a conference attendee
	I want to be able to use discount codes

@discount
Scenario: Get a percentage discount
	Given the event of creating a conference has occurred
	And the event of adding a discount with scope all for 20 % has occurred
	When the command to apply this discount to a total of $1000 is received 
	Then the event of total changed to $800 is emmitted

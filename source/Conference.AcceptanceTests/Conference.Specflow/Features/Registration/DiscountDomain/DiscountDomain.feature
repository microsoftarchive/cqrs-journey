Feature: Discounts
	In order to save money
	As a conference attendee
	I want to be able to use discount codes

@discount
Scenario: Add discount code to conference
	Given the event of creating a conference has occurred
	When the command to create a discount is received
	Then the event of that discount being created is emmitted

@discount
Scenario: Get a percentage discount
	Given the event of creating a conference has occurred
	And the event of adding a discount with scope all for 20 % has occurred
	When the command to apply this discount to a total of $1000 is received 
	Then the event $200 discount has been applied is emmitted
    And the event corresponds to the discount requested

@discount
Scenario: Can't get a percentage discount twice
	Given the event of creating a conference has occurred
	And the event of adding a discount has occurred
	And the event of redeeming this discount has occurred
	When the command to apply this discount to any total is received
	Then a Discounts.Exceptions.DiscountAlreadyAppliedException is raised

@discount
Scenario: Get a different percentage discount
	Given the event of creating a conference has occurred
	And the event of adding a discount with scope all for 50 % has occurred
	When the command to apply this discount to a total of $1000 is received 
	Then the event $500 discount has been applied is emmitted
    And the event corresponds to the discount requested


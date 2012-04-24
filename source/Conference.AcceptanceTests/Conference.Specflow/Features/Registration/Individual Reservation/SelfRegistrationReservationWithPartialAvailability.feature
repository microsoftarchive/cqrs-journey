Feature: Self Registrant scenarios for making a Reservation for a Conference site with Order Items partially available
	In order to reserve Seats for a Conference
	As an Attendee
	I want to be able to select an Order Item from one or many of the available and or waitlisted Order Items and make a Reservation

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
	| General admission                | 1		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 1		  |
	When the Registrant proceed to make the Reservation			
	Then these Order Itmes get confirmed being waitlisted
	| seat type                        | quantity |
	| General admission                | 1		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 1		  |	
	And these waitlist registration details will be asked
	| waitlist registration details |
	| First Name                    |
	| Last Name                     |
	| Email Address                 |


#2
#Initial state	: 3 waitlisted and 2 selected
#End state		: 2 waitlisted confirmed  
Scenario: All order items are waitlisted and 2 are selected and all get confirmed	
	Given the list of Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| General admission                | 1		  |
	| Pre-con Workshop with Greg Young | 1		  |
	| Additional cocktail party		   | 0		  |
	When the Registrant proceed to make the Reservation			
	Then these Order Itmes get confirmed being waitlisted
	| seat type                        | waitlist seats |
	| General admission                | 1			    |
	| Pre-con Workshop with Greg Young | 1				|


#3
#Initial state	: 1 available, 2 waitlisted and 3 selected
#End state		: 1 reserved,  2 waitlisted confirmed  
Scenario: 1 order item is available, 2 are waitlisted and all are selected, then all get confirmed	
	Given the list of available Order Items selected by the Registrant
	| seat type                        | quantity |
	| General admission                | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 1        |	
	When the Registrant proceed to make the Reservation					
	Then these order itmes get confirmed being waitlisted
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 1        |
	And these other order items get reserved
	| seat type         | quantity |
	| General admission | 1        |


#4
#Initial state	: 1 available, 2 waitlisted but only 2w selected
#End state		: 2 waitlisted confirmed  
Scenario: 1 order item is available, 2 are waitlisted and 2 are selected, then 2 get confirmed	
	Given the list of available Order Items selected by the Registrant
	| seat type                        | quantity |
	| General admission                | 0        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 1        |	
	When the Registrant proceed to make the Reservation					
	Then these order itmes get confirmed being waitlisted
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 1        |


#5
#Initial state	: 1 available, 2 waitlisted and only 1a selected
#End state		: 1 reserved 
Scenario: 1 order item is available,  2 are waitlisted and 1 available is selected, then only 1 get reserved	
	Given the list of available Order Items selected by the Registrant
	| seat type                        | quantity |
	| General admission                | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 0        |
	| Additional cocktail party        | 0        |	
	When the Registrant proceed to make the Reservation					
	Then these order items get reserved
	| seat type         | quantity |
	| General admission | 1        |


#6
#Initial state	: 1 available, 2 waitlisted and 1a & 1w selected
#End state		: 1 reserved,  1 waitlisted confirmed  
Scenario: 1 order item is available, 2 are waitlisted, 1 available and 1 waitlisted are selected, then 1 get reserved and 1 get waitlisted	
	Given the list of available Order Items selected by the Registrant
	| seat type                        | quantity |
	| General admission                | 1        |
	And the list of these Order Items offered to be waitlisted and selected by the Registrant
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1        |
	| Additional cocktail party        | 0        |	
	When the Registrant proceed to make the Reservation					
	Then these order itmes get confirmed being waitlisted
	| seat type                        | quantity |
	| Pre-con Workshop with Greg Young | 1        |
	And these other order items get reserved
	| seat type         | quantity |
	| General admission | 1        |


#7
Scenario: No selected Seat Type
When the Registrant proceed to make the Reservation		
Then the message 'One or more items are required' will show up


#8
Scenario: Zero seats selected
Given the selected Order Items
	| seat type                 | quantity |
	| General admission         | 0        |
When the Registrant proceed to make the Reservation		
Then the message 'The Quantity field is required.' will show up



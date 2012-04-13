Feature: Registrant workflow for assigning all the registered Order Items.

Background: 
	Given the individual purchased Seats
	| First name | Last name | email address         | Seat type                 |
	| John       | Smith     | johnsmith@contoso.com | General admission         |
	| John       | Smith     | johnsmith@contoso.com | Additional cocktail party |
	And the group purchased Seats
	| First name | Last name | email address     | Seat type                        |
	| William    | Smith     | William@Smith.com | General admission                |
	| John       | Doe       | JohnDoe@live.com  | General admission                |
	| Oliver     | Smith     | Oliver@Smith.com  | Pre-con Workshop with Greg Young |
	| Tim        | Martin    | Tim@Martin.com    | Pre-con Workshop with Greg Young |
	| Mani       | Kris      | Mani@Kris.com     | Additional cocktail party        |
	| Jim        | John      | Jim@John.com      | Additional cocktail party        |


Scenario: Allocate all purchased Seats for an individual
Given the ConfirmSuccessfulRegistration for the selected Order Items
And the Order Access code is 6789
And the Registrant assign the infividual purchased Seats to attendees as following
	| First name | Last name | email address         | Seat type                 |
	| John       | Smith     | johnsmith@contoso.com | General admission         |
	| John       | Smith     | johnsmith@contoso.com | Additional cocktail party |
Then the Registrant should get a Seat assignment confirmation
And the Attendees should get an email informing about the conference and the Seat Type with Seat Access Code
	| Access code | email address         | Seat type                 |
	| 6789-1      | johnsmith@contoso.com | General admission         |
	| 6789-2      | johnsmith@contoso.com | Additional cocktail party |

	
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

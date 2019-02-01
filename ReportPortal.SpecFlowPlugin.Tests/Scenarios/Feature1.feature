Feature: Feature1
	In order to avoid silly mistakes
	As a math idiot
	I want to be told the sum of two numbers

@mytag
Scenario: Add two numbers
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen
	When I upload "cat.png" into Report Portal

@mytag @super_super_tag
Scenario: Add three numbers
	Given I have entered 3 into the calculator
	And I have entered 7 into the calculator
	And I have entered 8 into the calculator
	When I press add
	Then the result should be 18 on the screen
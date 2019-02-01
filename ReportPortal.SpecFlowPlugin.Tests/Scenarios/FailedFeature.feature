Feature: Failed
	Scenarios should be failed

Scenario: Failed test
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 0 on the screen

Feature: Pass
	All scenarios in this feature are passed

@mytag
Scenario: Pass Scenario 1
	Given I have entered 50 into the calculator
	And I have entered 70 into the calculator
	When I press add
	Then the result should be 120 on the screen

@mytag @super_super_tag
Scenario: Pass Scenario 2
	Given I have entered 3 into the calculator
	And I have entered 7 into the calculator
	When I press add
	Then the result should be 10 on the screen

Scenario Outline: Parametrized scenario 3
this is scenario outline description
	Given I have entered <a> into the calculator
	And I have entered <b> into the calculator

	Examples:
		| a | b  |
		| 1 | 11 |
		| 2 | 22 |
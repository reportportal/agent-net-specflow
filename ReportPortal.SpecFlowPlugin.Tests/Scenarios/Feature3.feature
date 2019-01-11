Feature: Feature3

Scenario: System Error
	Then I execute failed test

Scenario Outline: Parametrized scenario
	Given I have entered <a> into the calculator
	And I have entered <b> into the calculator
	When I press add
	Then the result should be <c> on the screen

	Examples: 
	| a | b | c   |
	| 1 | 2 | 3   |
	| 2 | 2 | 4   |
	| 3 | 3 | 666 |
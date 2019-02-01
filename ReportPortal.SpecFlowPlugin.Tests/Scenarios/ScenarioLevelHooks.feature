Feature: Scenario Level Hooks
	

@should_fail_before
Scenario: Should Fail In ScenarioBefore hook
	Given I have entered 50 into the calculator
	
@should_fail_after
Scenario: Should Fail In ScenarioAfter hook
	Given I have entered 50 into the calculator

Scenario: Should Pass
	Given I have entered 50 into the calculator

Scenario: Should Fail
	Then I execute failed step
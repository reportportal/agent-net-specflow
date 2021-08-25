Feature: Scenario Level Hooks
	

@scenario_should_fail_before
Scenario: Should Fail In ScenarioBefore hook
	Given I have entered 50 into the calculator
	
@scenario_should_fail_after
Scenario: Should Fail In ScenarioAfter hook
	Given I have entered 50 into the calculator

Scenario: Should Pass
	Given I have entered 50 into the calculator

Scenario: Should fail in step
	Then I execute failed step

@step_should_fail_before
Scenario: Should fail because of step fails before
	Given I have entered 50 into the calculator
	When I press add

@step_should_fail_after
Scenario: Should fail because of step fails after
	Given I have entered 50 into the calculator
	When I press add

@scenario_should_ignore_before_runtime
Scenario: Should Be Ignored
	Given I have entered 50 into the calculator

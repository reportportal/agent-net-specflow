using ReportPortal.Shared;
using System;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.IntegrationTests
{
    [Binding]
    public sealed class StepDefinition
    {
        [Given("I have entered (.*) into the calculator")]
        public void GivenIHaveEnteredSomethingIntoTheCalculator(int number)
        {
            Log.Debug($"Executing {nameof(GivenIHaveEnteredSomethingIntoTheCalculator)} step");
        }

        [When("I press add")]
        public void WhenIPressAdd()
        {
            Log.Debug($"Executing {nameof(WhenIPressAdd)} step");
        }

        [Then("the result should be (.*) on the screen")]
        public void ThenTheResultShouldBe(int result)
        {
            Log.Debug($"Executing {nameof(ThenTheResultShouldBe)} step");
        }

        [Then(@"I execute failed step")]
        public void ThenIExecuteFailedTest()
        {
            throw new Exception("This step raises an exception.");
        }

    }
}

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
            
        }

        [When("I press add")]
        public void WhenIPressAdd()
        {
            
        }

        [Then("the result should be (.*) on the screen")]
        public void ThenTheResultShouldBe(int result)
        {
            
        }

        [Then(@"I execute failed step")]
        public void ThenIExecuteFailedTest()
        {
            throw new Exception("This step raises an exception.");
        }

    }
}

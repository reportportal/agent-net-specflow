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
            Context.Current.Log.Debug($"Executing {nameof(GivenIHaveEnteredSomethingIntoTheCalculator)} step");

            using (var scope = Context.Current.Log.BeginScope("qwe"))
            {
                scope.Info("a");
                Context.Current.Log.Info("b");
                Context.Current.Log.Root.Info("root");

                scope.Status = Shared.Execution.Logging.LogScopeStatus.Skipped;
            }
        }

        [When("I press add")]
        public void WhenIPressAdd()
        {
            Context.Current.Log.Debug($"Executing {nameof(WhenIPressAdd)} step");
        }

        [Then("the result should be (.*) on the screen")]
        public void ThenTheResultShouldBe(int result)
        {
            Context.Current.Log.Debug($"Executing {nameof(ThenTheResultShouldBe)} step");
        }

        [Then(@"I execute failed step")]
        public void ThenIExecuteFailedTest()
        {
            throw new Exception("This step raises an exception.");
        }

    }
}

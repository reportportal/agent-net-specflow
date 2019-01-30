using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TechTalk.SpecFlow;

namespace Example.SpecFlow.StepDefenitions
{
    [Binding]
    public sealed class StepDefinition1
    {
        // For additional details on SpecFlow step definitions see http://go.specflow.org/doc-stepdef
        [When(@"I upload ""(.*)"" into Report Portal")]
        public void WhenIUploadIntoReportPortal(string fileName)
        {
            //var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + fileName;
            //Bridge.LogMessage(ReportPortal.Client.Models.LogLevel.Info, "this is my cat {rp#file#" + filePath + "}");
        }


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
            if (result == 666)
            {
                throw new Exception("Daemon here.");
            }
        }

        [Then(@"I execute failed test")]
        public void ThenIExecuteFailedTest()
        {
            throw new Exception("This step raises an exception.");
        }

    }
}

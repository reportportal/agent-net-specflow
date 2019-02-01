using System;
using System.IO;
using System.Reflection;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.IntegrationTests
{
    [Binding]
    public sealed class Hooks
    {
        [BeforeFeature("should_fail_before")]
        public static void BeforeFeatureShouldFail()
        {
            throw new Exception("This feature should fail before.");
        }

        [AfterFeature("should_fail_after")]
        public static void AfterFeatureShouldFail()
        {
            throw new Exception("This feature should fail after.");
        }

        [BeforeScenario("should_fail_before")]
        public void BeforeScenarioShouldFail()
        {
            throw new Exception("This scenario should fail before.");
        }

        [AfterScenario("should_fail_after")]
        public void AfterScenarioShouldFail()
        {
            throw new Exception("This scenario should fail after.");
        }
    }
}

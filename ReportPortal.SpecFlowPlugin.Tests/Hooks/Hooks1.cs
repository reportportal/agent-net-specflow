using ReportPortal.Shared;
using ReportPortal.SpecFlowPlugin;
using ReportPortal.SpecFlowPlugin.EventArguments;
using System;
using System.IO;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.Tests.Hooks
{
    [Binding]
    public sealed class Hooks1
    {
        [BeforeScenario("666")]
        public void BeforeScenario(ScenarioContext context)
        {
            throw new Exception("Scenario before hook fails because of 666.");
        }
    }
}

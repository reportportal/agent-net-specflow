using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class TestItemFinishedEventArgs: EventArgs
    {
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, ITestReporter testReporter)
        {
            Service = service;
            FinishTestItemRequest = request;
            TestReporter = testReporter;
        }

        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, ITestReporter testReporter, FeatureContext featureContext, ScenarioContext scenarioContext)
            : this(service, request, testReporter)
        {
            FeatureContext = featureContext;
            ScenarioContext = scenarioContext;
        }

        public Service Service { get; }

        public FinishTestItemRequest FinishTestItemRequest { get; }

        public ITestReporter TestReporter { get; }

        public FeatureContext FeatureContext { get; }

        public ScenarioContext ScenarioContext { get; }

        public bool Canceled { get; set; }
    }
}

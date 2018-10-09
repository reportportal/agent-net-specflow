using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class TestItemFinishedEventArgs: EventArgs
    {
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, TestReporter testReporter)
        {
            Service = service;
            TestItem = request;
            TestReporter = testReporter;
        }

        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, TestReporter testReporter, FeatureContext featureContext, ScenarioContext scenarioContext)
            : this(service, request, testReporter)
        {
            FeatureContext = featureContext;
            ScenarioContext = scenarioContext;
        }

        public Service Service { get; }

        public FinishTestItemRequest TestItem { get; }

        public TestReporter TestReporter { get; }

        public FeatureContext FeatureContext { get; }

        public ScenarioContext ScenarioContext { get; }

        public bool Canceled { get; set; }
    }
}

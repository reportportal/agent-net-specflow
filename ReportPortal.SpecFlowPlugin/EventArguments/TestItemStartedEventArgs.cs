using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class TestItemStartedEventArgs : EventArgs
    {
        public TestItemStartedEventArgs(Service service, StartTestItemRequest request)
        {
            Service = service;
            StartTestItemRequest = request;
        }

        public TestItemStartedEventArgs(Service service, StartTestItemRequest request, ITestReporter testReporter)
            : this(service, request)
        {
            TestReporter = testReporter;
        }

        public TestItemStartedEventArgs(Service service, StartTestItemRequest request, ITestReporter testReporter, FeatureContext featureContext, ScenarioContext scenarioContext)
            : this(service, request, testReporter)
        {
            this.FeatureContext = featureContext;
            this.ScenarioContext = scenarioContext;
        }

        public Service Service { get; }

        public StartTestItemRequest StartTestItemRequest { get; }

        public ITestReporter TestReporter { get; }

        public FeatureContext FeatureContext { get; }

        public ScenarioContext ScenarioContext { get; }

        public bool Canceled { get; set;}
    }
}

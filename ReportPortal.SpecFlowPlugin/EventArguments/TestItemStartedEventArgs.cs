using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class TestItemStartedEventArgs : EventArgs
    {
        public TestItemStartedEventArgs(Service service, StartTestItemRequest request)
        {
            Service = service;
            TestItem = request;
        }

        public TestItemStartedEventArgs(Service service, StartTestItemRequest request, TestReporter testReporter)
            : this(service, request)
        {
            TestReporter = testReporter;
        }

        public TestItemStartedEventArgs(Service service, StartTestItemRequest request, TestReporter testReporter, FeatureContext featureContext, ScenarioContext scenarioContext)
            : this(service, request, testReporter)
        {
            this.FeatureContext = featureContext;
            this.ScenarioContext = scenarioContext;
        }

        public Service Service { get; }

        public StartTestItemRequest TestItem { get; }

        public TestReporter TestReporter { get; }

        public FeatureContext FeatureContext { get; }

        public ScenarioContext ScenarioContext { get; }

        public bool Canceled { get; set;}
    }
}

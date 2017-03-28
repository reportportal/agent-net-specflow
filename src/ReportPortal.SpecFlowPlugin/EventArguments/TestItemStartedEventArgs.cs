using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

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

        public Service Service { get; private set; }

        public StartTestItemRequest TestItem { get; private set; }

        public TestReporter TestReporter { get; private set; }

        public bool Canceled { get; set;}
    }
}

using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

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

        public Service Service { get; private set; }

        public FinishTestItemRequest TestItem { get; private set; }

        public TestReporter TestReporter { get; private set; }

        public bool Canceled { get; set; }
    }
}

using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class StepFinishedEventArgs : EventArgs
    {
        public StepFinishedEventArgs(Service service, AddLogItemRequest request, TestReporter testReporter)
        {
            Service = service;
            TestItem = request;
            TestReporter = testReporter;
        }

        public Service Service { get; }

        public AddLogItemRequest TestItem { get; }

        public TestReporter TestReporter { get; }

        public bool Canceled { get; set; }
    }
}

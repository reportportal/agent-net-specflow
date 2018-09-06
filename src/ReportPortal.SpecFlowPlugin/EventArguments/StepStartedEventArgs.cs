using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class StepStartedEventArgs : EventArgs
    {
        public StepStartedEventArgs(Service service, AddLogItemRequest request)
        {
            Service = service;
            TestItem = request;
        }

        public StepStartedEventArgs(Service service, AddLogItemRequest request, TestReporter testReporter)
            : this(service, request)
        {
            TestReporter = testReporter;
        }

        public Service Service { get; }

        public AddLogItemRequest TestItem { get; }

        public TestReporter TestReporter { get; }

        public bool Canceled { get; set;}
    }
}

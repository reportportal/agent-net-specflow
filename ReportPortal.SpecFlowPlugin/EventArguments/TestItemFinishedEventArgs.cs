using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class TestItemFinishedEventArgs: EventArgs
    {
        private readonly Service _service;
        private readonly FinishTestItemRequest _request;
        private readonly string _message;
        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request)
        {
            _service = service;
            _request = request;
        }

        public TestItemFinishedEventArgs(Service service, FinishTestItemRequest request, string message)
            :this(service, request)
        {
            _message = message;
        }

        public Service Service
        {
            get { return _service; }
        }

        public FinishTestItemRequest TestItem
        {
            get { return _request; }
        }

        public string Message
        {
            get { return _message; }
        }

        public bool Canceled { get; set; }
    }
}

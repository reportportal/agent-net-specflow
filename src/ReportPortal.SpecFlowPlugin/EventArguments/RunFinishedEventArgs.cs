using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class RunFinishedEventArgs : EventArgs
    {
        public RunFinishedEventArgs(Service service, FinishLaunchRequest request, LaunchReporter launchReporter)
        {
            Service = service;
            Launch = request;
            LaunchReporter = launchReporter;
        }

        public Service Service { get; private set; }

        public FinishLaunchRequest Launch { get; private set; }

        public LaunchReporter LaunchReporter { get; private set; }

        public bool Canceled { get; set; }
    }
}

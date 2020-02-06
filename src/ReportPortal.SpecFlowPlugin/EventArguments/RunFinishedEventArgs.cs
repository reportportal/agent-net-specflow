using System;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class RunFinishedEventArgs : EventArgs
    {
        public RunFinishedEventArgs(IClientService service, FinishLaunchRequest request, ILaunchReporter launchReporter)
        {
            Service = service;
            FinishLaunchRequest = request;
            LaunchReporter = launchReporter;
        }

        public IClientService Service { get; }

        public FinishLaunchRequest FinishLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; }

        public bool Canceled { get; set; }
    }
}

using System;
using ReportPortal.Client;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class RunStartedEventArgs : EventArgs
    {
        public RunStartedEventArgs(Service service, StartLaunchRequest request)
        {
            Service = service;
            StartLaunchRequest = request;
        }

        public RunStartedEventArgs(Service service, StartLaunchRequest request, ILaunchReporter launchReporter)
            : this(service, request)
        {
            LaunchReporter = launchReporter;
        }

        public Service Service { get; }

        public StartLaunchRequest StartLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; set; }

        public bool Canceled { get; set; }
    }
}

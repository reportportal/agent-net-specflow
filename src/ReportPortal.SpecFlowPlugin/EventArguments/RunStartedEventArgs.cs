using System;
using ReportPortal.Client;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class RunStartedEventArgs : EventArgs
    {
        public RunStartedEventArgs(IClientService service, StartLaunchRequest request)
        {
            Service = service;
            StartLaunchRequest = request;
        }

        public RunStartedEventArgs(IClientService service, StartLaunchRequest request, ILaunchReporter launchReporter)
            : this(service, request)
        {
            LaunchReporter = launchReporter;
        }

        public IClientService Service { get; }

        public StartLaunchRequest StartLaunchRequest { get; }

        public ILaunchReporter LaunchReporter { get; set; }

        public bool Canceled { get; set; }
    }
}

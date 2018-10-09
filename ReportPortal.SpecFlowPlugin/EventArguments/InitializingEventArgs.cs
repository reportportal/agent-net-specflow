using ReportPortal.Client;
using ReportPortal.SpecFlowPlugin.Configuration;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class InitializingEventArgs
    {
        public InitializingEventArgs(Server server)
        {
            Server = server;
        }

        public Server Server { get; set; }

        public Service Service { get; set; }
    }
}

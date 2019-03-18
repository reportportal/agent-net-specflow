using ReportPortal.Client;
using ReportPortal.Shared.Configuration;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class InitializingEventArgs
    {
        public InitializingEventArgs(IConfiguration config)
        {
            Config = config;
        }

        public IConfiguration Config { get; set; }

        public Service Service { get; set; }
    }
}

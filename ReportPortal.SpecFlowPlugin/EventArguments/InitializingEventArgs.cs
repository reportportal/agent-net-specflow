using ReportPortal.Client;
using ReportPortal.SpecFlowPlugin.Configuration;

namespace ReportPortal.SpecFlowPlugin.EventArguments
{
    public class InitializingEventArgs
    {
        public InitializingEventArgs(Config config)
        {
            Config = config;
        }

        public Config Config { get; set; }

        public Service Service { get; set; }
    }
}

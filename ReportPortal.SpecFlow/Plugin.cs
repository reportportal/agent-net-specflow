using System;
using BoDi;
using ReportPortal.SpecFlow;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.SpecFlow
{
    /// <summary>
    /// Registered SpecFlow plugin from configuration file.
    /// </summary>
    public class Plugin: IRuntimePlugin
    {
        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters)
        {
            if (Configuration.ReportPortal.Enabled)
            {
                // container.RegisterTypeAs<ReportPortalAddin, ITestTracer>();
            }
        }
    }
}

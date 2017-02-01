using ReportPortal.SpecFlowPlugin;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.SpecFlowPlugin
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
                runtimePluginEvents.CustomizeTestThreadDependencies += (sender, e) =>
                {
                    e.ObjectContainer.RegisterTypeAs<ReportPortalAddin, ITestTracer>();
                };

                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IBindingInvoker>();
                };
            }
        }
    }
}

using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.SpecFlowPlugin;
using System;
using System.IO;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.UnitTestProvider;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.SpecFlowPlugin
{
    /// <summary>
    /// Registered SpecFlow plugin from configuration file.
    /// </summary>
    internal class Plugin : IRuntimePlugin
    {
        private ITraceLogger _traceLogger;

        public static IConfiguration Config { get; set; }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            var currentDirectory = Path.GetDirectoryName(new Uri(typeof(Plugin).Assembly.CodeBase).LocalPath);

            _traceLogger = TraceLogManager.Instance.WithBaseDir(currentDirectory).GetLogger<Plugin>();

            Config = new ConfigurationBuilder().AddDefaults(currentDirectory).Build();

            var isEnabled = Config.GetValue("Enabled", true);

            if (isEnabled)
            {
                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    e.SpecFlowConfiguration.AdditionalStepAssemblies.Add("ReportPortal.SpecFlowPlugin");
                    e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IBindingInvoker>();
                };

                runtimePluginEvents.CustomizeScenarioDependencies += (sender, e) =>
                {
                    e.ObjectContainer.RegisterTypeAs<SkippedStepsHandler, ISkippedStepHandler>();
                };
            }
        }
    }
}

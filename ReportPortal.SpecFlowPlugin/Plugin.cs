using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using ReportPortal.SpecFlowPlugin;
using System;
using System.IO;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Plugins;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.SpecFlowPlugin
{
    /// <summary>
    /// Registered SpecFlow plugin from configuration file.
    /// </summary>
    internal class Plugin : IRuntimePlugin
    {
        public static IConfiguration Config { get; set; }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters)
        {
            var jsonPath = Path.GetDirectoryName(new Uri(typeof(Plugin).Assembly.CodeBase).LocalPath) + "/ReportPortal.config.json";

            Config = new ConfigurationBuilder().AddJsonFile(jsonPath).AddEnvironmentVariables().Build();

            var isEnabled = Config.GetValue("Enabled", true);

            if (isEnabled)
            {
                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    e.SpecFlowConfiguration.AdditionalStepAssemblies.Add("ReportPortal.SpecFlowPlugin");
                };

                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IBindingInvoker>();
                };
            }
        }
    }
}

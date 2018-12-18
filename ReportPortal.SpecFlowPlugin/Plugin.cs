using ReportPortal.SpecFlowPlugin;
using ReportPortal.SpecFlowPlugin.Configuration;
using System;
using System.IO;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.UnitTestProvider;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.SpecFlowPlugin
{
    /// <summary>
    /// Registered SpecFlow plugin from configuration file.
    /// </summary>
    internal class Plugin: IRuntimePlugin
    {
        public static Config Config { get; set; }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            var configPath = Path.GetDirectoryName(new Uri(typeof(Config).Assembly.CodeBase).LocalPath) + "/ReportPortal.config.json";
            Config = Client.Converters.ModelSerializer.Deserialize<Config>(File.ReadAllText(configPath));

            if (Config.IsEnabled)
            {
                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IBindingInvoker>();
                    e.SpecFlowConfiguration.AdditionalStepAssemblies.Add("ReportPortal.SpecFlowPlugin");
                };
            }
        }
    }
}

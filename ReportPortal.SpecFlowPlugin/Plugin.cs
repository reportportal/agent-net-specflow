using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using ReportPortal.SpecFlowPlugin;
using System;
using System.Diagnostics;
using System.IO;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.UnitTestProvider;

[assembly: RuntimePlugin(typeof(Plugin))]
namespace ReportPortal.SpecFlowPlugin
{
    /// <summary>
    /// Registered SpecFlow plugin from configuration file.
    /// </summary>
    internal class Plugin: IRuntimePlugin
    {
        public static IConfiguration Config { get; set; }

        public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters, UnitTestProviderConfiguration unitTestProviderConfiguration)
        {
            var jsonPath = Path.GetDirectoryName(new Uri(typeof(Plugin).Assembly.CodeBase).LocalPath) + "/ReportPortal.config.json";

            Config = new ConfigurationBuilder().AddJsonFile(jsonPath).AddEnvironmentVariables().Build();

            var isEnabled = Config.GetValue("Enabled", true);

            if (isEnabled)
            {
                runtimePluginEvents.CustomizeGlobalDependencies += (sender, e) =>
                {
                    //e.ObjectContainer.RegisterTypeAs<SafeBindingInvoker, IBindingInvoker>();
                    //e.SpecFlowConfiguration.AdditionalStepAssemblies.Add("ReportPortal.SpecFlowPlugin");

                    //e.ObjectContainer.RegisterTypeAs<W_NewReportPortalAddin, ITestExecutionEngine>();
                };

                runtimePluginEvents.CustomizeTestThreadDependencies += RuntimePluginEvents_CustomizeTestThreadDependencies;
            }
        }

        private void RuntimePluginEvents_CustomizeTestThreadDependencies(object sender, CustomizeTestThreadDependenciesEventArgs e)
        {
            e.ObjectContainer.RegisterTypeAs<W_NewTestRunner, ITestRunner>();
            e.ObjectContainer.RegisterTypeAs<W_NewReportPortalAddin, ITestExecutionEngine>();
            e.ObjectContainer.RegisterTypeAs<W_NewTraceListener, ITraceListener>();
        }
    }
}

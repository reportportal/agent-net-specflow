using System;
using System.Diagnostics;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace ReportPortal.SpecFlowPlugin
{
    public class SafeBindingInvoker : BindingInvoker
    {
        public SafeBindingInvoker(RuntimeConfiguration runtimeConfiguration, IErrorProvider errorProvider)
            : base(runtimeConfiguration, errorProvider)
        {
        }

        public override object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments,
            ITestTracer testTracer, out TimeSpan duration)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                return base.InvokeBinding(binding, contextManager, arguments,
                    testTracer, out duration);
            }
            catch (Exception ex)
            {
                PreserveStackTrace(ex);

                if (binding is IHookBinding == false)
                {
                    throw;
                }

                var hookBinding = binding as IHookBinding;

                if (hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeScenarioBlock
                    || hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeStep
                    || hookBinding.HookType == HookType.AfterStep
                    || hookBinding.HookType == HookType.AfterScenario
                    || hookBinding.HookType == HookType.AfterScenarioBlock)
                {
                    testTracer.TraceError(ex);
                    SetTestError(contextManager.ScenarioContext, ex);
                }
            }
            finally
            {
                stopwatch.Stop();

                duration = stopwatch.Elapsed;
            }

            return new object();
        }

        private static void SetTestError(ScenarioContext context, Exception ex)
        {
            if (context != null && context.TestError == null)
            {
                context.GetType().GetProperty("TestStatus", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(context, TestStatus.TestError);

                context.GetType().GetProperty("TestError")
                    .SetValue(context, ex);
            }
        }

        private static void PreserveStackTrace(Exception ex)
        {
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ex, new object[0]);
        }
    }
}

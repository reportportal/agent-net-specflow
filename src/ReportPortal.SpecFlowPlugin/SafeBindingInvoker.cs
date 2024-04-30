using ReportPortal.Shared;
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
    internal class SafeBindingInvoker : BindingInvoker
    {
        public SafeBindingInvoker(SpecFlowConfiguration specFlowConfiguration, IErrorProvider errorProvider, ISynchronousBindingDelegateInvoker synchronousBindingDelegateInvoker)
            : base(specFlowConfiguration, errorProvider, synchronousBindingDelegateInvoker)
        {
        }

        public override object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments,
            ITestTracer testTracer, out TimeSpan duration)
        {
            object result = null;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                if (IsStepInFailedFeature(binding, contextManager.FeatureContext))
                {
                    // Pass the FeatureContext TestError on to the ScenarioContext to mark the scenario as failed
                    SetTestError(contextManager.ScenarioContext, contextManager.FeatureContext.TestError);
                }
                else
                {
                    result = base.InvokeBinding(binding, contextManager, arguments,
                        testTracer, out duration);
                }
            }
            catch (Exception ex)
            {
                PreserveStackTrace(ex);

                if (binding is IHookBinding == false)
                {
                    throw;
                }

                var hookBinding = binding as IHookBinding;

                stopwatch.Stop();
                duration = stopwatch.Elapsed;

                testTracer.TraceError(ex, duration);

                if (hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeScenarioBlock
                    || hookBinding.HookType == HookType.BeforeScenario
                    || hookBinding.HookType == HookType.BeforeStep
                    || hookBinding.HookType == HookType.AfterStep
                    || hookBinding.HookType == HookType.AfterScenario
                    || hookBinding.HookType == HookType.AfterScenarioBlock)
                {
                    SetTestError(contextManager.ScenarioContext, ex);
                }
                else if (hookBinding.HookType == HookType.BeforeFeature)
                {
                    SetTestError(contextManager.FeatureContext, ex);
                }
                else if (hookBinding.HookType == HookType.BeforeTestRun)
                {
                    // throw to fail entire test run
                    throw;
                }
            }
            finally
            {
                stopwatch.Stop();

                duration = stopwatch.Elapsed;
            }

            return result;
        }

        private static bool IsStepInFailedFeature(IBinding binding, FeatureContext featureContext)
        {
            return featureContext?.TestError != null && binding is IStepDefinitionBinding;
        }

        private static bool IsStepInFailedTestRun(IBinding binding, TestThreadContext testThreadContext)
        {
            return testThreadContext?.TestError != null && binding is IStepDefinitionBinding;
        }

        private static void SetTestError(ScenarioContext context, Exception ex)
        {
            if (context != null && context.TestError == null)
            {
                context.GetType().GetProperty("ScenarioExecutionStatus")
                    ?.SetValue(context, ScenarioExecutionStatus.TestError);

                context.GetType().GetProperty("TestError")
                    ?.SetValue(context, ex);
            }
        }

        private void SetTestError(FeatureContext context, Exception ex)
        {
            if (context != null && context.TestError == null)
            {
                context.GetType().GetProperty("TestError")
                    ?.SetValue(context, ex);
            }
        }

        private static void PreserveStackTrace(Exception ex)
        {
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(ex, new object[0]);
        }
    }
}

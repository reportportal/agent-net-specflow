using System;
using System.Collections.Concurrent;
using System.Linq;
using ReportPortal.Shared;
using ReportPortal.Shared.Reporter;
using ReportPortal.SpecFlowPlugin.EventArguments;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    public class ReportPortalAddin
    {
        private static readonly ConcurrentDictionary<FeatureInfo, ITestReporter> FeatureTestReporters = new ConcurrentDictionary<FeatureInfo, ITestReporter>(new FeatureInfoEqualityComparer());

        private static readonly ConcurrentDictionary<FeatureInfo, int> FeatureThreadCount = new ConcurrentDictionary<FeatureInfo, int>(new FeatureInfoEqualityComparer());

        private static readonly ConcurrentDictionary<ScenarioInfo, ITestReporter> ScenarioTestReporters = new ConcurrentDictionary<ScenarioInfo, ITestReporter>();

        [Obsolete("Use thread-safe method GetFeatureTestReporter to get the current feature TestReporter.")]
        public static ITestReporter CurrentFeature => FeatureTestReporters.Select(kv => kv.Value).LastOrDefault(reporter => reporter.FinishTask == null);

        [Obsolete("Use thread-safe method GetScenarioTestReporter to get the current scenario TestReporter.")]
        public static ITestReporter CurrentScenario => ScenarioTestReporters.Select(kv => kv.Value).LastOrDefault(reporter => reporter.FinishTask == null);

        [Obsolete]
        public static string CurrentScenarioDescription { get; } = string.Empty;

        public static ITestReporter GetFeatureTestReporter(FeatureContext context)
        {
            return FeatureTestReporters.ContainsKey(context.FeatureInfo) ? FeatureTestReporters[context.FeatureInfo] : null;
        }

        internal static void SetFeatureTestReporter(FeatureContext context, ITestReporter reporter)
        {
            FeatureTestReporters[context.FeatureInfo] = reporter;
            FeatureThreadCount[context.FeatureInfo] = 1;
        }

        internal static void RemoveFeatureTestReporter(FeatureContext context, ITestReporter reporter)
        {
            FeatureTestReporters.TryRemove(context.FeatureInfo, out reporter);
        }

        internal static int IncrementFeatureThreadCount(FeatureContext context)
        {
            return FeatureThreadCount[context.FeatureInfo]
                = FeatureThreadCount.ContainsKey(context.FeatureInfo) ? FeatureThreadCount[context.FeatureInfo] + 1 : 1;
        }

        internal static int DecrementFeatureThreadCount(FeatureContext context)
        {
            return FeatureThreadCount[context.FeatureInfo] 
                = FeatureThreadCount.ContainsKey(context.FeatureInfo) ? FeatureThreadCount[context.FeatureInfo] - 1 : 0;
        }

        public static ITestReporter GetScenarioTestReporter(ScenarioContext context)
        {
            return ScenarioTestReporters.ContainsKey(context.ScenarioInfo) ? ScenarioTestReporters[context.ScenarioInfo] : null;
        }

        internal static void SetScenarioTestReporter(ScenarioContext context, ITestReporter reporter)
        {
            ScenarioTestReporters[context.ScenarioInfo] = reporter;
        }

        internal static void RemoveScenarioTestReporter(ScenarioContext context, ITestReporter reporter)
        {
            ScenarioTestReporters.TryRemove(context.ScenarioInfo, out reporter);
        }

        public delegate void InitializingHandler(object sender, InitializingEventArgs e);

        public static event InitializingHandler Initializing;

        internal static void OnInitializing(object sender, InitializingEventArgs eventArg)
        {
            Initializing?.Invoke(sender, eventArg);
        }

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);

        public static event RunStartedHandler BeforeRunStarted;
        public static event RunStartedHandler AfterRunStarted;

        internal static void OnBeforeRunStarted(object sender, RunStartedEventArgs eventArg)
        {
            BeforeRunStarted?.Invoke(sender, eventArg);
        }

        internal static void OnAfterRunStarted(object sender, RunStartedEventArgs eventArg)
        {
            AfterRunStarted?.Invoke(sender, eventArg);
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);

        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

        internal static void OnBeforeRunFinished(object sender, RunFinishedEventArgs eventArg)
        {
            BeforeRunFinished?.Invoke(sender, eventArg);
        }

        internal static void OnAfterRunFinished(object sender, RunFinishedEventArgs eventArg)
        {
            AfterRunFinished?.Invoke(sender, eventArg);
        }

        public delegate void FeatureStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event FeatureStartedHandler BeforeFeatureStarted;
        public static event FeatureStartedHandler AfterFeatureStarted;

        internal static void OnBeforeFeatureStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            BeforeFeatureStarted?.Invoke(sender, eventArg);
        }

        internal static void OnAfterFeatureStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            AfterFeatureStarted?.Invoke(sender, eventArg);
        }

        public delegate void FeatureFinishedHandler(object sender, TestItemFinishedEventArgs e);

        public static event FeatureFinishedHandler BeforeFeatureFinished;
        public static event FeatureFinishedHandler AfterFeatureFinished;

        internal static void OnBeforeFeatureFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            BeforeFeatureFinished?.Invoke(sender, eventArg);
        }

        internal static void OnAfterFeatureFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            AfterFeatureFinished?.Invoke(sender, eventArg);
        }

        public delegate void ScenarioStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event ScenarioStartedHandler BeforeScenarioStarted;
        public static event ScenarioStartedHandler AfterScenarioStarted;

        internal static void OnBeforeScenarioStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            BeforeScenarioStarted?.Invoke(sender, eventArg);
        }

        internal static void OnAfterScenarioStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            AfterScenarioStarted?.Invoke(sender, eventArg);
        }

        public delegate void ScenarioFinishedHandler(object sender, TestItemFinishedEventArgs e);

        public static event ScenarioFinishedHandler BeforeScenarioFinished;
        public static event ScenarioFinishedHandler AfterScenarioFinished;

        internal static void OnBeforeScenarioFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            BeforeScenarioFinished?.Invoke(sender, eventArg);
        }

        internal static void OnAfterScenarioFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            AfterScenarioFinished?.Invoke(sender, eventArg);
        }

        public delegate void StepStartedHandler(object sender, StepStartedEventArgs e);

        public static event StepStartedHandler BeforeStepStarted;
        public static event StepStartedHandler AfterStepStarted;

        internal static void OnBeforeStepStarted(object sender, StepStartedEventArgs eventArg)
        {
            BeforeStepStarted?.Invoke(sender, eventArg);
        }

        internal static void OnAfterStepStarted(object sender, StepStartedEventArgs eventArg)
        {
            AfterStepStarted?.Invoke(sender, eventArg);
        }

        public delegate void StepFinishedHandler(object sender, StepFinishedEventArgs e);

        public static event StepFinishedHandler BeforeStepFinished;
        public static event StepFinishedHandler AfterStepFinished;

        internal static void OnBeforeStepFinished(object sender, StepFinishedEventArgs eventArg)
        {
            BeforeStepFinished?.Invoke(sender, eventArg);
        }

        internal static void OnAfterStepFinished(object sender, StepFinishedEventArgs eventArg)
        {
            AfterStepFinished?.Invoke(sender, eventArg);
        }
    }
}

﻿using System;
using System.Collections.Concurrent;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter;
using ReportPortal.SpecFlowPlugin.EventArguments;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    public class ReportPortalAddin
    {
        private static readonly ITraceLogger Logger = TraceLogManager.Instance.GetLogger<ReportPortalAddin>();

        private static ConcurrentDictionary<FeatureInfo, ITestReporter> FeatureTestReporters { get; } = new ConcurrentDictionary<FeatureInfo, ITestReporter>(new FeatureInfoEqualityComparer());

        private static ConcurrentDictionary<FeatureInfo, int> FeatureThreadCount { get; } = new ConcurrentDictionary<FeatureInfo, int>(new FeatureInfoEqualityComparer());

        private static ConcurrentDictionary<ScenarioInfo, ITestReporter> ScenarioTestReporters { get; } = new ConcurrentDictionary<ScenarioInfo, ITestReporter>();

        private static ConcurrentDictionary<StepInfo, ITestReporter> StepTestReporters { get; } = new ConcurrentDictionary<StepInfo, ITestReporter>();

        // key: log scope ID, value: according test reporter
        public static ConcurrentDictionary<string, ITestReporter> LogScopes { get; } = new ConcurrentDictionary<string, ITestReporter>();

        public static ITestReporter GetFeatureTestReporter(FeatureContext context)
        {
            if (context != null && FeatureTestReporters.ContainsKey(context.FeatureInfo))
            {
                return FeatureTestReporters[context.FeatureInfo];
            }
            else
            {
                return null;
            }
        }

        internal static void SetFeatureTestReporter(FeatureContext context, ITestReporter reporter)
        {
            FeatureTestReporters[context.FeatureInfo] = reporter;
            FeatureThreadCount[context.FeatureInfo] = 1;
        }

        internal static void RemoveFeatureTestReporter(FeatureContext context, ITestReporter reporter)
        {
            FeatureTestReporters.TryRemove(context.FeatureInfo, out reporter);
            FeatureThreadCount.TryRemove(context.FeatureInfo, out int count);
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
            if (context != null && ScenarioTestReporters.ContainsKey(context.ScenarioInfo))
            {
                return ScenarioTestReporters[context.ScenarioInfo];
            }
            else
            {
                return null;
            }
        }

        internal static void SetScenarioTestReporter(ScenarioContext context, ITestReporter reporter)
        {
            ScenarioTestReporters[context.ScenarioInfo] = reporter;
        }

        internal static void RemoveScenarioTestReporter(ScenarioContext context, ITestReporter reporter)
        {
            ScenarioTestReporters.TryRemove(context.ScenarioInfo, out reporter);
        }

        public static ITestReporter GetStepTestReporter(ScenarioStepContext context)
        {
            if (context != null && StepTestReporters.ContainsKey(context.StepInfo))
            {
                return StepTestReporters[context.StepInfo];
            }
            else
            {
                return null;
            }
        }

        internal static void SetStepTestReporter(ScenarioStepContext context, ITestReporter reporter)
        {
            StepTestReporters[context.StepInfo] = reporter;
        }

        internal static void RemoveStepTestReporter(ScenarioStepContext context, ITestReporter reporter)
        {
            StepTestReporters.TryRemove(context.StepInfo, out reporter);
        }

        public delegate void InitializingHandler(object sender, InitializingEventArgs e);

        public static event InitializingHandler Initializing;

        internal static void OnInitializing(object sender, InitializingEventArgs eventArg)
        {
            try
            {
                Initializing?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnInitializing)} event handler: {exp}");
            }
        }

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);

        public static event RunStartedHandler BeforeRunStarted;
        public static event RunStartedHandler AfterRunStarted;

        internal static void OnBeforeRunStarted(object sender, RunStartedEventArgs eventArg)
        {
            try
            {
                BeforeRunStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeRunStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterRunStarted(object sender, RunStartedEventArgs eventArg)
        {
            try
            {
                AfterRunStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterRunStarted)} event handler: {exp}");
            }
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);

        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

        internal static void OnBeforeRunFinished(object sender, RunFinishedEventArgs eventArg)
        {
            try
            {
                BeforeRunFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeRunFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterRunFinished(object sender, RunFinishedEventArgs eventArg)
        {
            try
            {
                AfterRunFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterRunFinished)} event handler: {exp}");
            }
        }

        public delegate void FeatureStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event FeatureStartedHandler BeforeFeatureStarted;
        public static event FeatureStartedHandler AfterFeatureStarted;

        internal static void OnBeforeFeatureStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            try
            {
                BeforeFeatureStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeFeatureStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterFeatureStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            try
            {
                AfterFeatureStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterFeatureStarted)} event handler: {exp}");
            }
        }

        public delegate void FeatureFinishedHandler(object sender, TestItemFinishedEventArgs e);

        public static event FeatureFinishedHandler BeforeFeatureFinished;
        public static event FeatureFinishedHandler AfterFeatureFinished;

        internal static void OnBeforeFeatureFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            try
            {
                BeforeFeatureFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeFeatureFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterFeatureFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            try
            {
                AfterFeatureFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterFeatureFinished)} event handler: {exp}");
            }
        }

        public delegate void ScenarioStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event ScenarioStartedHandler BeforeScenarioStarted;
        public static event ScenarioStartedHandler AfterScenarioStarted;

        internal static void OnBeforeScenarioStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            try
            {
                BeforeScenarioStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeScenarioStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterScenarioStarted(object sender, TestItemStartedEventArgs eventArg)
        {
            try
            {
                AfterScenarioStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterScenarioStarted)} event handler: {exp}");
            }
        }

        public delegate void ScenarioFinishedHandler(object sender, TestItemFinishedEventArgs e);

        public static event ScenarioFinishedHandler BeforeScenarioFinished;
        public static event ScenarioFinishedHandler AfterScenarioFinished;

        internal static void OnBeforeScenarioFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            try
            {
                BeforeScenarioFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeScenarioFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterScenarioFinished(object sender, TestItemFinishedEventArgs eventArg)
        {
            try
            {
                AfterScenarioFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterScenarioFinished)} event handler: {exp}");
            }
        }

        public delegate void StepStartedHandler(object sender, StepStartedEventArgs e);

        public static event StepStartedHandler BeforeStepStarted;
        public static event StepStartedHandler AfterStepStarted;

        internal static void OnBeforeStepStarted(object sender, StepStartedEventArgs eventArg)
        {
            try
            {
                BeforeStepStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeStepStarted)} event handler: {exp}");
            }
        }

        internal static void OnAfterStepStarted(object sender, StepStartedEventArgs eventArg)
        {
            try
            {
                AfterStepStarted?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterStepStarted)} event handler: {exp}");
            }
        }

        public delegate void StepFinishedHandler(object sender, StepFinishedEventArgs e);

        public static event StepFinishedHandler BeforeStepFinished;
        public static event StepFinishedHandler AfterStepFinished;

        internal static void OnBeforeStepFinished(object sender, StepFinishedEventArgs eventArg)
        {
            try
            {
                BeforeStepFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnBeforeStepFinished)} event handler: {exp}");
            }
        }

        internal static void OnAfterStepFinished(object sender, StepFinishedEventArgs eventArg)
        {
            try
            {
                AfterStepFinished?.Invoke(sender, eventArg);
            }
            catch (Exception exp)
            {
                Logger.Error($"Exception occured in {nameof(OnAfterStepFinished)} event handler: {exp}");
            }
        }
    }
}

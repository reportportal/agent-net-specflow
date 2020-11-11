using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Execution.Logging;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Extensibility.Commands;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter;
using System.Collections.Generic;
using System.Threading;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin.LogHandler
{
    public class ContextAwareLogHandler : ICommandsListener
    {
        private readonly ITraceLogger _traceLogger = TraceLogManager.Instance.GetLogger<ContextAwareLogHandler>();

        public void Initialize(ICommandsSource commandsSource)
        {
            commandsSource.OnBeginLogScopeCommand += CommandsSource_OnBeginLogScopeCommand;
            commandsSource.OnEndLogScopeCommand += CommandsSource_OnEndLogScopeCommand;
            commandsSource.OnLogMessageCommand += CommandsSource_OnLogMessageCommand;
        }

        private void CommandsSource_OnLogMessageCommand(Shared.Execution.ILogContext logContext, Shared.Extensibility.Commands.CommandArgs.LogMessageCommandArgs args)
        {
            var logScope = args.LogScope;

            ITestReporter testReporter;

            if (logScope != null && ReportPortalAddin.LogScopes.ContainsKey(logScope.Id))
            {
                testReporter = ReportPortalAddin.LogScopes[logScope.Id];
            }
            else
            {
                // TODO: investigate SpecFlow how to understand current scenario context
                testReporter = GetCurrentTestReporter();
            }

            if (testReporter != null)
            {
                testReporter.Log(args.LogMessage.ConvertToRequest());
            }
        }

        private void CommandsSource_OnBeginLogScopeCommand(Shared.Execution.ILogContext logContext, Shared.Extensibility.Commands.CommandArgs.LogScopeCommandArgs args)
        {
            var logScope = args.LogScope;

            var startRequest = new StartTestItemRequest
            {
                Name = logScope.Name,
                StartTime = logScope.BeginTime,
                HasStats = false
            };

            ITestReporter parentTestReporter;

            if (logScope.Parent != null)
            {
                parentTestReporter = ReportPortalAddin.LogScopes[logScope.Parent.Id];
            }
            else
            {
                parentTestReporter = GetCurrentTestReporter();
            }

            if (parentTestReporter != null)
            {
                var nestedStep = parentTestReporter.StartChildTestReporter(startRequest);
                ReportPortalAddin.LogScopes[logScope.Id] = nestedStep;
            }
            else
            {
                _traceLogger.Warn("Unknown current step context to begin new log scope.");
            }
        }

        private void CommandsSource_OnEndLogScopeCommand(Shared.Execution.ILogContext logContext, Shared.Extensibility.Commands.CommandArgs.LogScopeCommandArgs args)
        {
            var logScope = args.LogScope;

            var finishRequest = new FinishTestItemRequest
            {
                EndTime = logScope.EndTime.Value,
                Status = _nestedStepStatusMap[logScope.Status]
            };

            if (ReportPortalAddin.LogScopes.ContainsKey(logScope.Id))
            {
                ReportPortalAddin.LogScopes[logScope.Id].Finish(finishRequest);
                ReportPortalAddin.LogScopes.Remove(logScope.Id);
            }
            else
            {
                _traceLogger.Warn($"Unknown current step context to end log scope with `{logScope.Id}` ID.");
            }
        }

        private static readonly AsyncLocal<ScenarioStepContext> _activeStepContext = new AsyncLocal<ScenarioStepContext>();

        public static ScenarioStepContext ActiveStepContext
        {
            get
            {
                return _activeStepContext.Value;
            }
            set
            {
                _activeStepContext.Value = value;
            }
        }

        private static readonly AsyncLocal<ScenarioContext> _activeScenarioContext = new AsyncLocal<ScenarioContext>();

        public static ScenarioContext ActiveScenarioContext
        {
            get
            {
                return _activeScenarioContext.Value;
            }
            set
            {
                _activeScenarioContext.Value = value;
            }
        }

        private static readonly AsyncLocal<FeatureContext> _activeFeatureContext = new AsyncLocal<FeatureContext>();

        public static FeatureContext ActiveFeatureContext
        {
            get
            {
                return _activeFeatureContext.Value;
            }
            set
            {
                _activeFeatureContext.Value = value;
            }
        }

        public ITestReporter GetCurrentTestReporter()
        {
            var testReporter = ReportPortalAddin.GetStepTestReporter(ActiveStepContext);

            if (testReporter == null)
            {
                testReporter = ReportPortalAddin.GetScenarioTestReporter(ActiveScenarioContext);
            }

            if (testReporter == null)
            {
                testReporter = ReportPortalAddin.GetFeatureTestReporter(ActiveFeatureContext);
            }

            return testReporter;
        }

        private Dictionary<LogScopeStatus, Status> _nestedStepStatusMap = new Dictionary<LogScopeStatus, Status> {
            { LogScopeStatus.InProgress, Status.InProgress },
            { LogScopeStatus.Passed, Status.Passed },
            { LogScopeStatus.Failed, Status.Failed },
            { LogScopeStatus.Skipped,Status.Skipped }
        };
    }
}

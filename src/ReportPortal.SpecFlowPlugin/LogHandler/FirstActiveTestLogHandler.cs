using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Logging;
using ReportPortal.Shared.Reporter;
using System.Collections.Generic;

namespace ReportPortal.SpecFlowPlugin.LogHandler
{
    public class FirstActiveTestLogHandler : ILogHandler
    {
        private readonly ITraceLogger _traceLogger = TraceLogManager.Instance.GetLogger<FirstActiveTestLogHandler>();

        public int Order => 10;

        public void BeginScope(ILogScope logScope)
        {
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

        public void EndScope(ILogScope logScope)
        {
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

        public bool Handle(ILogScope logScope, CreateLogItemRequest logRequest)
        {
            _traceLogger.Verbose("Identifying test context of log message...");

            ITestReporter testReporter;

            if (logScope != null)
            {
                testReporter = ReportPortalAddin.LogScopes[logScope.Id];
            }
            else
            {
                // TODO: investigate SpecFlow how to understand current scenario context
                testReporter = GetCurrentTestReporter();
            }
            var handled = false;

            if (testReporter != null)
            {
                testReporter.Log(logRequest);

                handled = true;
            }

            return handled;
        }

        public ITestReporter GetCurrentTestReporter()
        {
            var testReporter = ReportPortalAddin.GetStepTestReporter(TechTalk.SpecFlow.ScenarioStepContext.Current);

            if (testReporter == null)
            {
                testReporter = ReportPortalAddin.GetScenarioTestReporter(TechTalk.SpecFlow.ScenarioContext.Current);
            }

            if (testReporter == null)
            {
                testReporter = ReportPortalAddin.GetFeatureTestReporter(TechTalk.SpecFlow.FeatureContext.Current);
            }

            return testReporter;
        }

        private Dictionary<LogScopeStatus, Status> _nestedStepStatusMap = new Dictionary<Shared.Logging.LogScopeStatus, Status> {
            { LogScopeStatus.InProgress, Status.InProgress },
            { LogScopeStatus.Passed, Status.Passed },
            { LogScopeStatus.Failed, Status.Failed },
            { LogScopeStatus.Skipped,Status.Skipped }
        };
    }
}

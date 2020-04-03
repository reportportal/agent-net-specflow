using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Logging;
using ReportPortal.Shared.Reporter;

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

            var nestedStep = parentTestReporter.StartChildTestReporter(startRequest);
            ReportPortalAddin.LogScopes[logScope.Id] = nestedStep;
        }

        public void EndScope(ILogScope logScope)
        {
            var finishRequest = new FinishTestItemRequest
            {
                EndTime = logScope.EndTime.Value
            };

            ReportPortalAddin.LogScopes[logScope.Id].Finish(finishRequest);
            ReportPortalAddin.LogScopes.Remove(logScope.Id);
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
    }
}

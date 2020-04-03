using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Logging;

namespace ReportPortal.SpecFlowPlugin.LogHandler
{
    public class FirstActiveTestLogHandler : ILogHandler
    {
        private readonly ITraceLogger _traceLogger = TraceLogManager.Instance.GetLogger<FirstActiveTestLogHandler>();

        public int Order => int.MaxValue;

        public void BeginScope(ILogScope logScope)
        {

        }

        public void EndScope(ILogScope logScope)
        {

        }

        public bool Handle(ILogScope logScope, CreateLogItemRequest logRequest)
        {
            _traceLogger.Verbose("Identifying test context of log message...");

            var scenarioReporter = ReportPortalAddin.GetScenarioTestReporter(TechTalk.SpecFlow.ScenarioContext.Current);

            var handled = false;

            if (scenarioReporter != null)
            {
                scenarioReporter.Log(logRequest);

                handled = true;
            }

            return handled;
        }
    }
}

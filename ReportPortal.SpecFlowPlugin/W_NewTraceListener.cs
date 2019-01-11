using BoDi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Tracing;

namespace ReportPortal.SpecFlowPlugin
{
    public class W_NewTraceListener : ITraceListener
    {
        private readonly ITraceListenerQueue _traceListenerQueue;
        private readonly Lazy<ITestRunner> _testRunner;

        public W_NewTraceListener(ITraceListenerQueue traceListenerQueue, IObjectContainer container)
        {
            _traceListenerQueue = traceListenerQueue;
            _testRunner = new Lazy<ITestRunner>(container.Resolve<ITestRunner>);
        }

        public void WriteTestOutput(string message)
        {
            _traceListenerQueue.EnqueueMessage(_testRunner.Value, message, false);
        }

        public void WriteToolOutput(string message)
        {
            _traceListenerQueue.EnqueueMessage(_testRunner.Value, message, true);
        }
    }
}

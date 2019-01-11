using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Infrastructure;

namespace ReportPortal.SpecFlowPlugin
{
    public class W_NewTestRunner : ITestRunner
    {
        private readonly ITestRunner _runner;
        private readonly ITestExecutionEngine _engine;

        public W_NewTestRunner(ITestExecutionEngine engine)
        {
            _runner = new TestRunner(engine);
            _engine = engine;
        }

        public int ThreadId => _runner.ThreadId;

        public FeatureContext FeatureContext => _runner.FeatureContext;

        public ScenarioContext ScenarioContext => _runner.ScenarioContext;

        public void And(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            _runner.And(text, multilineTextArg, tableArg, keyword);
        }

        public void But(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            _runner.But(text, multilineTextArg, tableArg, keyword);
        }

        public void CollectScenarioErrors()
        {
            _runner.CollectScenarioErrors();
        }

        public void Given(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            _runner.Given(text, multilineTextArg, tableArg, keyword);
        }

        public void InitializeTestRunner(int threadId)
        {
            _runner.InitializeTestRunner(threadId);
        }

        public void OnFeatureEnd()
        {
            _runner.OnFeatureEnd();
        }

        public void OnFeatureStart(FeatureInfo featureInfo)
        {
            _runner.OnFeatureStart(featureInfo);
        }

        public void OnScenarioEnd()
        {
            _runner.OnScenarioEnd();
        }

        public void OnScenarioInitialize(ScenarioInfo scenarioInfo)
        {
            _runner.OnScenarioInitialize(scenarioInfo);
        }

        public void OnScenarioStart()
        {
            _runner.OnScenarioStart();
        }

        public void OnTestRunEnd()
        {
            _runner.OnTestRunEnd();
        }

        public void OnTestRunStart()
        {
            _runner.OnTestRunStart();
        }

        public void Pending()
        {
            _runner.Pending();
        }

        public void Then(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            _runner.Then(text, multilineTextArg, tableArg, keyword);
        }

        public void When(string text, string multilineTextArg, Table tableArg, string keyword = null)
        {
            _runner.When(text, multilineTextArg, tableArg, keyword);
        }
    }
}

using BoDi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.BindingSkeletons;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.UnitTestProvider;

namespace ReportPortal.SpecFlowPlugin
{
    public class W_NewTestExecutionEngine : ITestExecutionEngine
    {
        private TestExecutionEngine _engine;

        public W_NewTestExecutionEngine(
            IStepFormatter stepFormatter,
            ITestTracer testTracer,
            IErrorProvider errorProvider,
            IStepArgumentTypeConverter stepArgumentTypeConverter,
            SpecFlowConfiguration specFlowConfiguration,
            IBindingRegistry bindingRegistry,
            IUnitTestRuntimeProvider unitTestRuntimeProvider,
            IStepDefinitionSkeletonProvider stepDefinitionSkeletonProvider,
            IContextManager contextManager,
            IStepDefinitionMatchService stepDefinitionMatchService,
            IDictionary<string, IStepErrorHandler> stepErrorHandlers,
            IBindingInvoker bindingInvoker,
            IObsoleteStepHandler obsoleteStepHandler,
            ITestObjectResolver testObjectResolver,
            IObjectContainer objectContainer)
        {
            _engine = new TestExecutionEngine(stepFormatter,
                testTracer,
                errorProvider,
                stepArgumentTypeConverter,
                specFlowConfiguration,
                bindingRegistry,
                unitTestRuntimeProvider,
                stepDefinitionSkeletonProvider,
                contextManager,
                stepDefinitionMatchService,
                stepErrorHandlers,
                bindingInvoker,
                obsoleteStepHandler,
                testObjectResolver,
                objectContainer);
        }

        public FeatureContext FeatureContext => _engine.FeatureContext;

        public ScenarioContext ScenarioContext => _engine.ScenarioContext;

        public void OnAfterLastStep()
        {
            _engine.OnAfterLastStep();
        }

        public void OnFeatureEnd()
        {
            _engine.OnFeatureEnd();
        }

        public void OnFeatureStart(FeatureInfo featureInfo)
        {
            _engine.OnFeatureStart(featureInfo);
        }

        public void OnScenarioEnd()
        {
            _engine.OnScenarioEnd();
        }

        public void OnScenarioInitialize(ScenarioInfo scenarioInfo)
        {
            _engine.OnScenarioInitialize(scenarioInfo);
        }

        public void OnScenarioStart()
        {
            _engine.OnScenarioStart();
        }

        public void OnTestRunEnd()
        {
            System.Threading.Thread.Sleep(3000);
            _engine.OnTestRunEnd();
        }

        public void OnTestRunStart()
        {
            System.Threading.Thread.Sleep(3000);
            _engine.OnTestRunStart();
        }

        public void Pending()
        {
            _engine.Pending();
        }

        public void Step(StepDefinitionKeyword stepDefinitionKeyword, string keyword, string text, string multilineTextArg, Table tableArg)
        {
            _engine.Step(stepDefinitionKeyword, keyword, text, multilineTextArg, tableArg);
        }
    }
}

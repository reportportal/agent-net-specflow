﻿using System;
using System.IO;
using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.UnitTestProvider;

namespace ReportPortal.SpecFlowPlugin.IntegrationTests
{
    [Binding]
    public sealed class Hooks
    {
        private IUnitTestRuntimeProvider _unitTestRuntimeProvider;

        public Hooks(IUnitTestRuntimeProvider unitTestRuntimeProvider)
        {
            _unitTestRuntimeProvider = unitTestRuntimeProvider;
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            // all scenarios should fail (uncomment it to test)
            //throw new Exception("BeforeTestRun fail exception.");
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            // all scenarios should not be affected (uncomment it to test)
            //throw new Exception("AfterTestRun fail exception.");
        }

        [BeforeFeature("feature_should_fail_before")]
        public static void BeforeFeatureShouldFail()
        {
            throw new Exception("This feature should fail before.");
        }

        [AfterFeature("feature_should_fail_after")]
        public static void AfterFeatureShouldFail()
        {
            throw new Exception("This feature should fail after.");
        }

        [BeforeScenario("scenario_should_fail_before")]
        public void BeforeScenarioShouldFail()
        {
            throw new Exception("This scenario should fail before.");
        }

        [BeforeScenario("scenario_should_ignore_before_runtime")]
        public void BeforeScenarioShouldIgnore()
        {
            //_unitTestRuntimeProvider.TestIgnore("This scenario should be ignored at runtime.");
            _unitTestRuntimeProvider.TestInconclusive("This scenario should be ignored at runtime.");
        }

        [AfterScenario("scenario_should_fail_after")]
        public void AfterScenarioShouldFail()
        {
            throw new Exception("This scenario should fail after.");
        }

        [BeforeStep("step_should_fail_before")]
        public void BeforeStepShouldFail()
        {
            throw new Exception("This step should fail before.");
        }

        [AfterStep("step_should_fail_after")]
        public void AfterStepShouldFail()
        {
            throw new Exception("This step should fail after.");
        }
    }
}

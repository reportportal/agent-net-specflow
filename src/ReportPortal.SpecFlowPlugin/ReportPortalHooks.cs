using System;
using System.Collections.Generic;
using System.IO;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using ReportPortal.SpecFlowPlugin.EventArguments;
using ReportPortal.SpecFlowPlugin.Extensions;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    [Binding]
    internal class ReportPortalHooks : Steps
    {
        [BeforeTestRun(Order = -20000)]
        public static void BeforeTestRun()
        {
            if (Configuration.ReportPortal.Enabled)
            {
                var request = new StartLaunchRequest
                {
                    Name = Configuration.ReportPortal.Launch.Name,
                    StartTime = DateTime.UtcNow
                };

                if (Configuration.ReportPortal.Launch.DebugMode)
                {
                    request.Mode = LaunchMode.Debug;
                }

                request.Tags = new List<string>(Configuration.ReportPortal.Launch.Tags.Split(','));

                var eventArg = new RunStartedEventArgs(Bridge.Service, request);
                ReportPortalAddin.OnBeforeRunStarted(null, eventArg);

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter = new LaunchReporter(Bridge.Service);

                    Bridge.Context.LaunchReporter.Start(request);

                    ReportPortalAddin.OnAfterRunStarted(null, new RunStartedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                }
            }
        }

        [AfterTestRun(Order = 20000)]
        public static void AfterTestRun()
        {
            if (Bridge.Context.LaunchReporter != null)
            {
                var request = new FinishLaunchRequest
                {
                    EndTime = DateTime.UtcNow
                };

                var eventArg = new RunFinishedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter);
                ReportPortalAddin.OnBeforeRunFinished(null, eventArg);

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter.Finish(request);
                    try
                    {
                        Bridge.Context.LaunchReporter.FinishTask.Wait();
                    }
                    catch(Exception exp)
                    {
                        File.AppendAllText("ReportPortal.Errors.log", exp.ToString());
                    }

                    ReportPortalAddin.OnAfterRunFinished(null, new RunFinishedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                }
            }
        }

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            if (Bridge.Context.LaunchReporter != null)
            {
                lock (LockHelper.GetLock(FeatureInfoEqualityComparer.GetFeatureInfoHashCode(featureContext.FeatureInfo)))
                {
                    var currentFeature = ReportPortalAddin.GetFeatureTestReporter(featureContext);

                    if (currentFeature == null || currentFeature.FinishTask != null)
                    {
                        var request = new StartTestItemRequest
                        {
                            Name = featureContext.FeatureInfo.Title,
                            Description = featureContext.FeatureInfo.Description,
                            StartTime = DateTime.UtcNow,
                            Type = TestItemType.Suite,
                            Tags = new List<string>(featureContext.FeatureInfo.Tags)
                        };

                        var eventArg = new TestItemStartedEventArgs(Bridge.Service, request);
                        ReportPortalAddin.OnBeforeFeatureStarted(null, eventArg);

                        if (!eventArg.Canceled)
                        {
                            currentFeature = Bridge.Context.LaunchReporter.StartNewTestNode(request);
                            ReportPortalAddin.SetFeatureTestReporter(featureContext, currentFeature);

                            ReportPortalAddin.OnAfterFeatureStarted(null, new TestItemStartedEventArgs(Bridge.Service, request, currentFeature));
                        }
                    }
                    else
                    {
                        ReportPortalAddin.IncrementFeatureThreadCount(featureContext);
                    }
                }
            }
        }

        [AfterFeature(Order = 20000)]
        public static void AfterFeature(FeatureContext featureContext)
        {
            lock (LockHelper.GetLock(FeatureInfoEqualityComparer.GetFeatureInfoHashCode(featureContext.FeatureInfo)))
            {
                var currentFeature = ReportPortalAddin.GetFeatureTestReporter(featureContext);
                var remainingThreadCount = ReportPortalAddin.DecrementFeatureThreadCount(featureContext);

                if (currentFeature != null && currentFeature.FinishTask == null && remainingThreadCount == 0)
                {
                    var request = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = Status.Skipped
                    };

                    var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, currentFeature);
                    ReportPortalAddin.OnBeforeFeatureFinished(null, eventArg);

                    if (!eventArg.Canceled)
                    {
                        currentFeature.Finish(request);

                        ReportPortalAddin.OnAfterFeatureFinished(null, new TestItemFinishedEventArgs(Bridge.Service, request, currentFeature));
                    }
                }
            }
        }

        [BeforeScenario(Order = -20000)]
        public void BeforeScenario()
        {
            var currentFeature = ReportPortalAddin.GetFeatureTestReporter(this.FeatureContext);

            if (currentFeature != null)
            {
                var request = new StartTestItemRequest
                {
                    Name = this.ScenarioContext.ScenarioInfo.Title,
                    StartTime = DateTime.UtcNow,
                    Type = TestItemType.Step,
                    Tags = new List<string>(this.ScenarioContext.ScenarioInfo.Tags)
                };

                var eventArg = new TestItemStartedEventArgs(Bridge.Service, request);
                ReportPortalAddin.OnBeforeScenarioStarted(this, eventArg);

                if (!eventArg.Canceled)
                {
                    var currentScenario = currentFeature.StartNewTestNode(request);
                    ReportPortalAddin.SetScenarioTestReporter(this.ScenarioContext, currentScenario);

                    ReportPortalAddin.OnAfterScenarioStarted(this, new TestItemStartedEventArgs(Bridge.Service, request, currentFeature));
                }
            }
        }

        [AfterScenario(Order = 20000)]
        public void AfterScenario()
        {
            var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

            if (currentScenario != null)
            {
                Issue issue = null;
                var status = Status.Passed;

                switch (this.ScenarioContext.ScenarioExecutionStatus)
                {
                    case ScenarioExecutionStatus.TestError:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.ToInvestigate,
                            Comment = this.ScenarioContext.TestError?.Message
                        };

                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.ToString()
                        });
                        
                        break;
                    case ScenarioExecutionStatus.BindingError:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.AutomationBug,
                            Comment = this.ScenarioContext.TestError?.Message
                        };

                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.Message
                        });

                        break;
                    case ScenarioExecutionStatus.UndefinedStep:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.AutomationBug,
                            Comment = new MissingStepDefinitionException().Message
                        };

                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = new MissingStepDefinitionException().Message
                        });

                        break;
                    case ScenarioExecutionStatus.StepDefinitionPending:
                        status = Status.Failed;

                        issue = new Issue
                        {
                            Type = WellKnownIssueType.ToInvestigate,
                            Comment = "Pending"
                        };

                        break;
                }

                var request = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow.AddMilliseconds(1),
                    Status = status,
                    Issue = issue
                };

                var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, currentScenario);
                ReportPortalAddin.OnBeforeScenarioFinished(this, eventArg);

                if (!eventArg.Canceled)
                {
                    currentScenario.Finish(request);

                    ReportPortalAddin.OnAfterScenarioFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, currentScenario));
                }
            }
        }

        [BeforeStep(Order = -20000)]
        public void BeforeStep()
        {
            var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

            if (currentScenario != null)
            {
                var stepInfoRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Info,
                    Time = DateTime.UtcNow,
                    Text = this.StepContext.StepInfo.GetFullText()
                };

                var eventArg = new StepStartedEventArgs(Bridge.Service, stepInfoRequest, null);
                ReportPortalAddin.OnBeforeStepStarted(this, eventArg);

                if (!eventArg.Canceled)
                {
                    currentScenario.Log(stepInfoRequest);
                    ReportPortalAddin.OnAfterStepStarted(this, eventArg);
                }
            }
        }
        
        [AfterStep(Order = 20000)]
        public void AfterStep()
        {
            var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

            if (currentScenario != null)
            {
                var eventArg = new StepFinishedEventArgs(Bridge.Service, null, null);
                ReportPortalAddin.OnBeforeStepFinished(this, eventArg);

                if (!eventArg.Canceled)
                {
                    ReportPortalAddin.OnAfterStepFinished(this, eventArg);
                }
            }
        }
    }
}

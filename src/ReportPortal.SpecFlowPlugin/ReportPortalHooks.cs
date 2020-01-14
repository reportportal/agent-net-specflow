using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter;
using ReportPortal.SpecFlowPlugin.EventArguments;
using ReportPortal.SpecFlowPlugin.Extensions;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    [Binding]
    internal class ReportPortalHooks : Steps
    {
        private static readonly ITraceLogger Logger = TraceLogManager.GetLogger<ReportPortalHooks>();

        [BeforeTestRun(Order = -20000)]
        public static void BeforeTestRun()
        {
            try
            {
                var config = Initialize();

                var request = new StartLaunchRequest
                {
                    Name = config.GetValue(ConfigurationPath.LaunchName, "SpecFlow Launch"),
                    StartTime = DateTime.UtcNow
                };

                if (config.GetValue(ConfigurationPath.LaunchDebugMode, false))
                {
                    request.Mode = LaunchMode.Debug;
                }

                request.Tags = config.GetValues(ConfigurationPath.LaunchTags, new List<string>()).ToList();
                request.Description = config.GetValue(ConfigurationPath.LaunchDescription, string.Empty);

                var eventArg = new RunStartedEventArgs(Bridge.Service, request);
                ReportPortalAddin.OnBeforeRunStarted(null, eventArg);

                if (eventArg.LaunchReporter != null)
                {
                    Bridge.Context.LaunchReporter = eventArg.LaunchReporter;
                }

                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter = Bridge.Context.LaunchReporter ?? new LaunchReporter(Bridge.Service, config, null);

                    Bridge.Context.LaunchReporter.Start(request);

                    ReportPortalAddin.OnAfterRunStarted(null, new RunStartedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));

                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        private static IConfiguration Initialize()
        {
            var args = new InitializingEventArgs(Plugin.Config);

            ReportPortalAddin.OnInitializing(typeof(ReportPortalHooks), args);

            var uri = Plugin.Config.GetValue<string>(ConfigurationPath.ServerUrl);
            var project = Plugin.Config.GetValue<string>(ConfigurationPath.ServerProject); ;
            var uuid = Plugin.Config.GetValue<string>(ConfigurationPath.ServerAuthenticationUuid); ;

            if (args.Service != null)
            {
                Bridge.Service = args.Service;
            }
            else
            {
                Bridge.Service = new Service(new Uri(uri), project, uuid);
            }

            return args.Config;
        }

        [AfterTestRun(Order = 20000)]
        public static void AfterTestRun()
        {
            try
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

                        var sw = Stopwatch.StartNew();

                        Logger.Info($"Finishing to send results to ReportPortal...");
                        Bridge.Context.LaunchReporter.Sync();
                        Logger.Info($"Elapsed: {sw.Elapsed}{Environment.NewLine}");

                        ReportPortalAddin.OnAfterRunFinished(null, new RunFinishedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            try
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

                            var eventArg = new TestItemStartedEventArgs(Bridge.Service, request, null, featureContext, null);
                            ReportPortalAddin.OnBeforeFeatureStarted(null, eventArg);

                            if (!eventArg.Canceled)
                            {
                                currentFeature = Bridge.Context.LaunchReporter.StartChildTestReporter(request);
                                ReportPortalAddin.SetFeatureTestReporter(featureContext, currentFeature);

                                ReportPortalAddin.OnAfterFeatureStarted(null, new TestItemStartedEventArgs(Bridge.Service, request, currentFeature, featureContext, null));
                            }
                        }
                        else
                        {
                            ReportPortalAddin.IncrementFeatureThreadCount(featureContext);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterFeature(Order = 20000)]
        public static void AfterFeature(FeatureContext featureContext)
        {
            try
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

                        var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, currentFeature, featureContext, null);
                        ReportPortalAddin.OnBeforeFeatureFinished(null, eventArg);

                        if (!eventArg.Canceled)
                        {
                            currentFeature.Finish(request);

                            ReportPortalAddin.OnAfterFeatureFinished(null, new TestItemFinishedEventArgs(Bridge.Service, request, currentFeature, featureContext, null));
                        }

                        ReportPortalAddin.RemoveFeatureTestReporter(featureContext, currentFeature);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [BeforeScenario(Order = -20000)]
        public void BeforeScenario()
        {
            try
            {
                var currentFeature = ReportPortalAddin.GetFeatureTestReporter(this.FeatureContext);

                if (currentFeature != null)
                {
                    var request = new StartTestItemRequest
                    {
                        Name = this.ScenarioContext.ScenarioInfo.Title,
                        Description = this.ScenarioContext.ScenarioInfo.Description,
                        StartTime = DateTime.UtcNow,
                        Type = TestItemType.Step,
                        Tags = new List<string>(this.ScenarioContext.ScenarioInfo.Tags)
                    };

                    var eventArg = new TestItemStartedEventArgs(Bridge.Service, request, currentFeature, this.FeatureContext, this.ScenarioContext);
                    ReportPortalAddin.OnBeforeScenarioStarted(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        var currentScenario = currentFeature.StartChildTestReporter(request);
                        ReportPortalAddin.SetScenarioTestReporter(this.ScenarioContext, currentScenario);

                        ReportPortalAddin.OnAfterScenarioStarted(this, new TestItemStartedEventArgs(Bridge.Service, request, currentFeature, this.FeatureContext, this.ScenarioContext));
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterScenario(Order = 20000)]
        public void AfterScenario()
        {
            try
            {
                var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

                if (currentScenario != null)
                {
                    if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                    {
                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.ToString()
                        });
                    }
                    else if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.BindingError)
                    {
                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.Message
                        });
                    }
                    else if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep)
                    {
                        currentScenario.Log(new AddLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = new MissingStepDefinitionException().Message
                        });
                    }

                    var status = this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.OK ? Status.Passed : Status.Failed;

                    var request = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = status
                    };

                    var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, currentScenario, this.FeatureContext, this.ScenarioContext);
                    ReportPortalAddin.OnBeforeScenarioFinished(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        currentScenario.Finish(request);

                        ReportPortalAddin.OnAfterScenarioFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, currentScenario, this.FeatureContext, this.ScenarioContext));

                        ReportPortalAddin.RemoveScenarioTestReporter(this.ScenarioContext, currentScenario);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [BeforeStep(Order = -20000)]
        public void BeforeStep()
        {
            try
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

                    var eventArg = new StepStartedEventArgs(Bridge.Service, stepInfoRequest, currentScenario, this.FeatureContext, this.ScenarioContext, this.StepContext);
                    ReportPortalAddin.OnBeforeStepStarted(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        currentScenario.Log(stepInfoRequest);
                        ReportPortalAddin.OnAfterStepStarted(this, eventArg);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }

        [AfterStep(Order = 20000)]
        public void AfterStep()
        {
            try
            {
                var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

                if (currentScenario != null)
                {
                    var eventArg = new StepFinishedEventArgs(Bridge.Service, null, currentScenario, this.FeatureContext, this.ScenarioContext, this.StepContext);
                    ReportPortalAddin.OnBeforeStepFinished(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        ReportPortalAddin.OnAfterStepFinished(this, eventArg);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.ToString());
            }
        }
    }
}

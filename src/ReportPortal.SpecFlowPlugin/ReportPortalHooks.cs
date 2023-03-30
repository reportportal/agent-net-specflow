using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ReportPortal.Client;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Abstractions.Responses;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Converters;
using ReportPortal.Shared.Internal.Logging;
using ReportPortal.Shared.Reporter;
using ReportPortal.SpecFlowPlugin.EventArguments;
using ReportPortal.SpecFlowPlugin.Extensions;
using ReportPortal.SpecFlowPlugin.LogHandler;
using TechTalk.SpecFlow;

namespace ReportPortal.SpecFlowPlugin
{
    [Binding]
    internal class ReportPortalHooks
    {
        private static readonly ITraceLogger _traceLogger = TraceLogManager.Instance.GetLogger<ReportPortalHooks>();

        private static IClientService _service;

        private static ILaunchReporter _launchReporter;

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

                request.Attributes = config.GetKeyValues("Launch:Attributes", new List<KeyValuePair<string, string>>()).Select(a => new ItemAttribute { Key = a.Key, Value = a.Value }).ToList();
                request.Description = config.GetValue(ConfigurationPath.LaunchDescription, string.Empty);

                var eventArg = new RunStartedEventArgs(_service, request);
                ReportPortalAddin.OnBeforeRunStarted(null, eventArg);

                if (eventArg.LaunchReporter != null)
                {
                    _launchReporter = eventArg.LaunchReporter;
                }

                if (!eventArg.Canceled)
                {
                    Shared.Extensibility.Embedded.Analytics.AnalyticsReportEventsObserver.DefineConsumer("agent-dotnet-specflow");

                    _launchReporter = _launchReporter ?? new LaunchReporter(_service, config, null, Shared.Extensibility.ExtensionManager.Instance);

                    _launchReporter.Start(request);

                    ReportPortalAddin.OnAfterRunStarted(null, new RunStartedEventArgs(_service, request, _launchReporter));
                }

                ReportPortalAddin.LaunchReporter = _launchReporter;
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
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
                _service = args.Service as Service;
            }
            else
            {
                _service = new Service(new Uri(uri), project, uuid);
            }

            return args.Config;
        }

        [AfterTestRun(Order = 20000)]
        public static void AfterTestRun()
        {
            try
            {
                if (_launchReporter != null)
                {
                    var request = new FinishLaunchRequest
                    {
                        EndTime = DateTime.UtcNow
                    };

                    var eventArg = new RunFinishedEventArgs(_service, request, _launchReporter);
                    ReportPortalAddin.OnBeforeRunFinished(null, eventArg);

                    if (!eventArg.Canceled)
                    {
                        _launchReporter.Finish(request);

                        var sw = Stopwatch.StartNew();

                        _traceLogger.Info($"Finishing to send results to ReportPortal...");
                        _launchReporter.Sync();
                        _traceLogger.Info($"Elapsed: {sw.Elapsed}");
                        _traceLogger.Info(_launchReporter.StatisticsCounter.ToString());

                        ReportPortalAddin.OnAfterRunFinished(null, new RunFinishedEventArgs(_service, request, _launchReporter));
                    }
                    else
                    {
                        var sw = Stopwatch.StartNew();

                        _traceLogger.Info($"Finishing to send results to ReportPortal...");

                        _launchReporter.Sync();

                        _traceLogger.Info($"Elapsed: {sw.Elapsed}");
                        _traceLogger.Info(_launchReporter.StatisticsCounter.ToString());
                    }
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
            }
        }

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            try
            {
                if (_launchReporter != null)
                {
                    ContextAwareLogHandler.ActiveFeatureContext = featureContext;

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
                                Attributes = featureContext.FeatureInfo.Tags?.Select(t => new ItemAttributeConverter().ConvertFrom(t, (opts) => opts.UndefinedKey = "Tag")).ToList()
                            };

                            var eventArg = new TestItemStartedEventArgs(_service, request, null, featureContext, null);
                            ReportPortalAddin.OnBeforeFeatureStarted(null, eventArg);

                            if (!eventArg.Canceled)
                            {
                                currentFeature = _launchReporter.StartChildTestReporter(request);
                                ReportPortalAddin.SetFeatureTestReporter(featureContext, currentFeature);

                                ReportPortalAddin.OnAfterFeatureStarted(null, new TestItemStartedEventArgs(_service, request, currentFeature, featureContext, null));
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
                _traceLogger.Error(exp.ToString());
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

                        var eventArg = new TestItemFinishedEventArgs(_service, request, currentFeature, featureContext, null);
                        ReportPortalAddin.OnBeforeFeatureFinished(null, eventArg);

                        if (!eventArg.Canceled)
                        {
                            currentFeature.Finish(request);

                            ReportPortalAddin.OnAfterFeatureFinished(null, new TestItemFinishedEventArgs(_service, request, currentFeature, featureContext, null));
                        }

                        ReportPortalAddin.RemoveFeatureTestReporter(featureContext, currentFeature);
                    }
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
            }
            finally
            {
                ContextAwareLogHandler.ActiveFeatureContext = null;
            }
        }

        [BeforeScenario(Order = -20000)]
        public void BeforeScenario(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            try
            {
                ContextAwareLogHandler.ActiveScenarioContext = scenarioContext;

                var currentFeature = ReportPortalAddin.GetFeatureTestReporter(featureContext);

                if (currentFeature != null)
                {
                    var request = new StartTestItemRequest
                    {
                        Name = scenarioContext.ScenarioInfo.Title,
                        Description = scenarioContext.ScenarioInfo.Description,
                        StartTime = DateTime.UtcNow,
                        Type = TestItemType.Step,
                        Attributes = scenarioContext.ScenarioInfo.Tags?.Select(t => new ItemAttributeConverter().ConvertFrom(t, (opts) => opts.UndefinedKey = "Tag")).ToList(),
                    };

                    // fetch scenario parameters (from Examples block)
                    var arguments = scenarioContext.ScenarioInfo.Arguments;
                    if (arguments != null && arguments.Count > 0)
                    {
                        request.Parameters = new List<KeyValuePair<string, string>>();

                        foreach (DictionaryEntry argument in arguments)
                        {
                            request.Parameters.Add(new KeyValuePair<string, string>
                            (
                                argument.Key.ToString(),
                                argument.Value.ToString()
                            ));
                        }

                        // append scenario outline parameters to description
                        var parametersInfo = new StringBuilder();
                        parametersInfo.Append("|");
                        foreach (var p in request.Parameters)
                        {
                            parametersInfo.Append(p.Key);

                            parametersInfo.Append("|");
                        }

                        parametersInfo.AppendLine();
                        parametersInfo.Append("|");
                        foreach (var p in request.Parameters)
                        {
                            parametersInfo.Append("---");
                            parametersInfo.Append("|");
                        }

                        parametersInfo.AppendLine();
                        parametersInfo.Append("|");
                        foreach (var p in request.Parameters)
                        {
                            parametersInfo.Append("**");
                            parametersInfo.Append(p.Value);
                            parametersInfo.Append("**");

                            parametersInfo.Append("|");
                        }

                        if (string.IsNullOrEmpty(request.Description))
                        {
                            request.Description = parametersInfo.ToString();
                        }
                        else
                        {
                            request.Description = parametersInfo.ToString() + Environment.NewLine + Environment.NewLine + request.Description;
                        }
                    }

                    var eventArg = new TestItemStartedEventArgs(_service, request, currentFeature, featureContext, scenarioContext);
                    ReportPortalAddin.OnBeforeScenarioStarted(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        var currentScenario = currentFeature.StartChildTestReporter(request);
                        ReportPortalAddin.SetScenarioTestReporter(scenarioContext, currentScenario);

                        ReportPortalAddin.OnAfterScenarioStarted(this, new TestItemStartedEventArgs(_service, request, currentFeature, featureContext, scenarioContext));
                    }
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
            }
        }

        [AfterScenario(Order = 20000)]
        public void AfterScenario(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            try
            {
                var currentScenario = ReportPortalAddin.GetScenarioTestReporter(scenarioContext);

                if (currentScenario != null)
                {
                    if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                    {
                        currentScenario.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = scenarioContext.TestError?.ToString()
                        });
                    }
                    else if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.BindingError)
                    {
                        currentScenario.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = scenarioContext.TestError?.Message
                        });
                    }
                    else if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep)
                    {
                        currentScenario.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = new MissingStepDefinitionException().Message
                        });
                    }

                    Issue issue = null;

                    // determine scenario status
                    var status = scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.OK ? Status.Passed : Status.Failed;

                    // handle well-known unit framework's ignore exceptions
                    if (scenarioContext.TestError != null)
                    {
                        var testErrorException = scenarioContext.TestError.GetType();

                        if (testErrorException.FullName.Equals("NUnit.Framework.IgnoreException")
                            || testErrorException.FullName.Equals("NUnit.Framework.InconclusiveException")
                            || testErrorException.FullName.Equals("Microsoft.VisualStudio.TestTools.UnitTesting.AssertInconclusiveException")
                            || testErrorException.FullName.Equals("Xunit.SkipException"))
                        {
                            status = Status.Skipped;
                            issue = new Issue
                            {
                                Type = WellKnownIssueType.NotDefect,
                                Comment = scenarioContext.TestError.Message
                            };
                        }
                    }

                    var request = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = status,
                        Issue = issue
                    };

                    var eventArg = new TestItemFinishedEventArgs(_service, request, currentScenario, featureContext, scenarioContext);
                    ReportPortalAddin.OnBeforeScenarioFinished(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        currentScenario.Finish(request);

                        ReportPortalAddin.OnAfterScenarioFinished(this, new TestItemFinishedEventArgs(_service, request, currentScenario, featureContext, scenarioContext));

                        ReportPortalAddin.RemoveScenarioTestReporter(scenarioContext, currentScenario);
                    }
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
            }
            finally
            {
                ContextAwareLogHandler.ActiveScenarioContext = null;
            }
        }

        [BeforeStep(Order = -20000)]
        public void BeforeStep(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            var stepContext = scenarioContext.StepContext;

            try
            {
                ContextAwareLogHandler.ActiveStepContext = stepContext;

                var currentScenario = ReportPortalAddin.GetScenarioTestReporter(scenarioContext);

                var stepInfoRequest = new StartTestItemRequest
                {
                    Name = stepContext.StepInfo.GetCaption(),
                    StartTime = DateTime.UtcNow,
                    HasStats = false
                };

                var eventArg = new StepStartedEventArgs(_service, stepInfoRequest, currentScenario, featureContext, scenarioContext, stepContext);
                ReportPortalAddin.OnBeforeStepStarted(this, eventArg);

                if (!eventArg.Canceled)
                {
                    var stepReporter = currentScenario.StartChildTestReporter(stepInfoRequest);
                    ReportPortalAddin.SetStepTestReporter(stepContext, stepReporter);

                    // step parameters
                    var formattedParameters = stepContext.StepInfo.GetFormattedParameters();
                    if (!string.IsNullOrEmpty(formattedParameters))
                    {
                        stepReporter.Log(new CreateLogItemRequest
                        {
                            Text = formattedParameters,
                            Level = LogLevel.Info,
                            Time = DateTime.UtcNow
                        });
                    }

                    ReportPortalAddin.OnAfterStepStarted(this, eventArg);
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
            }
        }

        [AfterStep(Order = 20000)]
        public void AfterStep(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            var stepContext = scenarioContext.StepContext;

            try
            {
                var currentStep = ReportPortalAddin.GetStepTestReporter(stepContext);

                var stepFinishRequest = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow
                };

                if (scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                {
                    stepFinishRequest.Status = Status.Failed;
                }

                var eventArg = new StepFinishedEventArgs(_service, stepFinishRequest, currentStep, featureContext, scenarioContext, stepContext);
                ReportPortalAddin.OnBeforeStepFinished(this, eventArg);

                if (!eventArg.Canceled)
                {
                    currentStep.Finish(stepFinishRequest);
                    ReportPortalAddin.RemoveStepTestReporter(stepContext, currentStep);
                    ReportPortalAddin.OnAfterStepFinished(this, eventArg);
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
            }
            finally
            {
                ContextAwareLogHandler.ActiveStepContext = null;
            }
        }
    }
}

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
    internal class ReportPortalHooks : Steps
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
                    Shared.Extensibility.Analytics.AnalyticsReportEventsObserver.DefineConsumer("agent-dotnet-specflow");

                    _launchReporter = _launchReporter ?? new LaunchReporter(_service, config, null, Shared.Extensibility.ExtensionManager.Instance);

                    _launchReporter.Start(request);

                    ReportPortalAddin.OnAfterRunStarted(null, new RunStartedEventArgs(_service, request, _launchReporter));

                }
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
        public void BeforeScenario()
        {
            try
            {
                ContextAwareLogHandler.ActiveScenarioContext = this.ScenarioContext;

                var currentFeature = ReportPortalAddin.GetFeatureTestReporter(this.FeatureContext);

                if (currentFeature != null)
                {
                    var request = new StartTestItemRequest
                    {
                        Name = this.ScenarioContext.ScenarioInfo.Title,
                        Description = this.ScenarioContext.ScenarioInfo.Description,
                        StartTime = DateTime.UtcNow,
                        Type = TestItemType.Step,
                        Attributes = this.ScenarioContext.ScenarioInfo.Tags?.Select(t => new ItemAttributeConverter().ConvertFrom(t, (opts) => opts.UndefinedKey = "Tag")).ToList(),
                    };

                    // fetch scenario parameters (from Examples block)
                    var arguments = this.ScenarioContext.ScenarioInfo.Arguments;
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

                    var eventArg = new TestItemStartedEventArgs(_service, request, currentFeature, this.FeatureContext, this.ScenarioContext);
                    ReportPortalAddin.OnBeforeScenarioStarted(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        var currentScenario = currentFeature.StartChildTestReporter(request);
                        ReportPortalAddin.SetScenarioTestReporter(this.ScenarioContext, currentScenario);

                        ReportPortalAddin.OnAfterScenarioStarted(this, new TestItemStartedEventArgs(_service, request, currentFeature, this.FeatureContext, this.ScenarioContext));
                    }
                }
            }
            catch (Exception exp)
            {
                _traceLogger.Error(exp.ToString());
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
                        currentScenario.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.ToString()
                        });
                    }
                    else if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.BindingError)
                    {
                        currentScenario.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Time = DateTime.UtcNow,
                            Text = this.ScenarioContext.TestError?.Message
                        });
                    }
                    else if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.UndefinedStep)
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
                    var status = this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.OK ? Status.Passed : Status.Failed;

                    // handle well-known unit framework's ignore exceptions
                    if (this.ScenarioContext.TestError != null)
                    {
                        var testErrorException = this.ScenarioContext.TestError.GetType();

                        if (testErrorException.FullName.Equals("NUnit.Framework.IgnoreException")
                            || testErrorException.FullName.Equals("NUnit.Framework.InconclusiveException"))
                        {
                            status = Status.Skipped;
                            issue = new Issue
                            {
                                Type = WellKnownIssueType.NotDefect,
                                Comment = this.ScenarioContext.TestError.Message
                            };
                        }
                    }

                    var request = new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = status,
                        Issue = issue
                    };

                    var eventArg = new TestItemFinishedEventArgs(_service, request, currentScenario, this.FeatureContext, this.ScenarioContext);
                    ReportPortalAddin.OnBeforeScenarioFinished(this, eventArg);

                    if (!eventArg.Canceled)
                    {
                        currentScenario.Finish(request);

                        ReportPortalAddin.OnAfterScenarioFinished(this, new TestItemFinishedEventArgs(_service, request, currentScenario, this.FeatureContext, this.ScenarioContext));

                        ReportPortalAddin.RemoveScenarioTestReporter(this.ScenarioContext, currentScenario);
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
        public void BeforeStep()
        {
            try
            {
                ContextAwareLogHandler.ActiveStepContext = this.StepContext;

                var currentScenario = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);

                var stepInfoRequest = new StartTestItemRequest
                {
                    Name = this.StepContext.StepInfo.GetCaption(),
                    StartTime = DateTime.UtcNow,
                    HasStats = false
                };

                var eventArg = new StepStartedEventArgs(_service, stepInfoRequest, currentScenario, this.FeatureContext, this.ScenarioContext, this.StepContext);
                ReportPortalAddin.OnBeforeStepStarted(this, eventArg);

                if (!eventArg.Canceled)
                {
                    var stepReporter = currentScenario.StartChildTestReporter(stepInfoRequest);
                    ReportPortalAddin.SetStepTestReporter(this.StepContext, stepReporter);

                    // step parameters
                    var formattedParameters = this.StepContext.StepInfo.GetFormattedParameters();
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
        public void AfterStep()
        {
            try
            {
                var currentStep = ReportPortalAddin.GetStepTestReporter(this.StepContext);

                var stepFinishRequest = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow
                };

                if (this.ScenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.TestError)
                {
                    stepFinishRequest.Status = Status.Failed;
                }

                var eventArg = new StepFinishedEventArgs(_service, stepFinishRequest, currentStep, this.FeatureContext, this.ScenarioContext, this.StepContext);
                ReportPortalAddin.OnBeforeStepFinished(this, eventArg);

                if (!eventArg.Canceled)
                {
                    currentStep.Finish(stepFinishRequest);
                    ReportPortalAddin.RemoveStepTestReporter(this.StepContext, currentStep);
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

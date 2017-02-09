using System;
using System.Collections.Generic;
using System.Net;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared;
using ReportPortal.SpecFlowPlugin.EventArguments;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Tracing;

namespace ReportPortal.SpecFlowPlugin
{
    [Binding]
    public class ReportPortalAddin : ITestTracer
    {
        public static string CurrentFeatureId { get; private set; }

        public static string CurrentScenarioId { get; private set; }

        public static string CurrentStepId { get; private set; }

        public static string CurrentScenarioDescription { get; private set; }

        static ReportPortalAddin()
        {
            var uri = new Uri(Configuration.ReportPortal.Server.Url);
            var project = Configuration.ReportPortal.Server.Project;
            var username = Configuration.ReportPortal.Server.Authentication.Username;
            var password = Configuration.ReportPortal.Server.Authentication.Password;

            IWebProxy proxy = null;

            if (Configuration.ReportPortal.Server.Proxy.ElementInformation.IsPresent)
            {
                proxy = new WebProxy(Configuration.ReportPortal.Server.Proxy.Server);
            }

            Bridge.Service = proxy == null ? new Service(uri, project, password) : new Service(uri, project, password, proxy);
        }

        public delegate void RunStartedHandler(object sender, RunStartedEventArgs e);
        public static event RunStartedHandler BeforeRunStarted;
        public static event RunStartedHandler AfterRunStarted;

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
                if (BeforeRunStarted != null) BeforeRunStarted(null, eventArg);
                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchId = Bridge.Service.StartLaunch(request).Id;
                    if (AfterRunStarted != null)
                        AfterRunStarted(null, new RunStartedEventArgs(Bridge.Service, request, Bridge.Context.LaunchId));
                }
            }
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);
        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

        [AfterTestRun(Order = 20000)]
        public static void AfterTestRun()
        {
            if (Bridge.Context.LaunchId != null)
            {
                var request = new FinishLaunchRequest
                {
                    EndTime = DateTime.UtcNow
                };

                var eventArg = new RunFinishedEventArgs(Bridge.Service, request);
                if (BeforeRunFinished != null) BeforeRunFinished(null, eventArg);
                if (!eventArg.Canceled)
                {
                    var message = Bridge.Service.FinishLaunch(Bridge.Context.LaunchId, request).Info;
                    if (AfterRunFinished != null)
                        AfterRunFinished(null, new RunFinishedEventArgs(Bridge.Service, request, message));

                    Bridge.Context.LaunchId = null;
                }
            }
        }

        public delegate void FeatureStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event FeatureStartedHandler BeforeFeatureStarted;
        public static event FeatureStartedHandler AfterFeatureStarted;

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature()
        {
            if (Bridge.Context.LaunchId != null)
            {
                var request = new StartTestItemRequest
                {
                    LaunchId = Bridge.Context.LaunchId,
                    Name = FeatureContext.Current.FeatureInfo.Title,
                    Description = FeatureContext.Current.FeatureInfo.Description,
                    StartTime = DateTime.UtcNow,
                    Type = TestItemType.Suite,
                    Tags = new List<string>(FeatureContext.Current.FeatureInfo.Tags)
                };

                var eventArg = new TestItemStartedEventArgs(Bridge.Service, request);
                if (BeforeFeatureStarted != null) BeforeFeatureStarted(null, eventArg);
                if (!eventArg.Canceled)
                {
                    CurrentFeatureId = Bridge.Service.StartTestItem(request).Id;
                    if (AfterFeatureStarted != null)
                        AfterFeatureStarted(null,
                                            new TestItemStartedEventArgs(Bridge.Service, request, CurrentFeatureId));
                }
            }
        }

        public delegate void FeatureFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event FeatureFinishedHandler BeforeFeatureFinished;
        public static event FeatureFinishedHandler AfterFeatureFinished;

        [AfterFeature(Order = 20000)]
        public static void AfterFeature()
        {
            if (CurrentFeatureId != null)
            {
                var request = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = Status.Skipped
                };

                var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request);
                if (BeforeFeatureFinished != null) BeforeFeatureFinished(null, eventArg);
                if (!eventArg.Canceled)
                {
                    var message = Bridge.Service.FinishTestItem(CurrentFeatureId, request).Info;
                    if (AfterFeatureFinished != null) AfterFeatureFinished(null, new TestItemFinishedEventArgs(Bridge.Service, request, message));

                    CurrentFeatureId = null;
                }
            }
        }

        public delegate void ScenarioStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event ScenarioStartedHandler BeforeScenarioStarted;
        public static event ScenarioStartedHandler AfterScenarioStarted;

        [BeforeScenario(Order = -20000)]
        public void BeforeScenario()
        {
            if (CurrentFeatureId != null)
            {
                CurrentScenarioDescription = string.Empty;

                Status = Status.Passed;
                var request = new StartTestItemRequest
                {
                    LaunchId = Bridge.Context.LaunchId,
                    Name = ScenarioContext.Current.ScenarioInfo.Title,
                    StartTime = DateTime.UtcNow,
                    Type = TestItemType.Step,
                    Description = CurrentScenarioDescription,
                    Tags = new List<string>(ScenarioContext.Current.ScenarioInfo.Tags)
                };

                var eventArg = new TestItemStartedEventArgs(Bridge.Service, request);
                if (BeforeScenarioStarted != null) BeforeScenarioStarted(this, eventArg);
                if (!eventArg.Canceled)
                {
                    CurrentScenarioId = Bridge.Service.StartTestItem(CurrentFeatureId, request).Id;
                    Bridge.Context.TestId = CurrentScenarioId;

                    if (AfterScenarioStarted != null)
                        AfterScenarioStarted(this,
                                             new TestItemStartedEventArgs(Bridge.Service, request, CurrentScenarioId));
                }
            }
        }

        public delegate void ScenarioFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event ScenarioFinishedHandler BeforeScenarioFinished;
        public static event ScenarioFinishedHandler AfterScenarioFinished;


        private static Status Status = Status.Passed;

        [AfterScenario(Order = 20000)]
        public void AfterScenario()
        {
            if (CurrentScenarioId != null)
            {
                var request = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = Status
                };

                var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request);
                if (BeforeScenarioFinished != null) BeforeScenarioFinished(this, eventArg);
                if (!eventArg.Canceled)
                {
                    var message = Bridge.Service.FinishTestItem(CurrentScenarioId, request).Info;
                    if (AfterScenarioFinished != null) AfterScenarioFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, message));

                    Bridge.Context.TestId = null;
                    CurrentScenarioId = null;
                    CurrentScenarioDescription = string.Empty;
                }
            }
        }

        public delegate void StepStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event StepStartedHandler BeforeStepStarted;
        public static event StepStartedHandler AfterStepStarted;
        public void TraceStep(StepInstance stepInstance, bool showAdditionalArguments)
        {
            if (CurrentScenarioId != null)
            {
                CurrentScenarioDescription += Environment.NewLine + stepInstance.Keyword + " " + stepInstance.Text;

                if (stepInstance.MultilineTextArgument != null)
                {
                    CurrentScenarioDescription += Environment.NewLine + stepInstance.MultilineTextArgument;
                }

                var tableDescription = string.Empty;
                if (stepInstance.TableArgument != null)
                {
                    tableDescription = string.Empty;
                    foreach (var header in stepInstance.TableArgument.Header)
                    {
                        tableDescription += "| " + header + "\t";
                    }
                    tableDescription += "|\n";
                    foreach (var row in stepInstance.TableArgument.Rows)
                    {
                        foreach (var value in row.Values)
                        {
                            tableDescription += "| " + value + "\t";
                        }
                        tableDescription += "|\n";
                    }
                }
                if (!string.IsNullOrEmpty(tableDescription))
                {
                    CurrentScenarioDescription += Environment.NewLine + tableDescription;
                }

                var updateScenarioRequest = new UpdateTestItemRequest
                {
                    Description = CurrentScenarioDescription
                };
                Bridge.Service.UpdateTestItem(CurrentScenarioId, updateScenarioRequest);


                var stepInfoRequest = new AddLogItemRequest
                {
                    TestItemId = CurrentScenarioId,
                    Level = LogLevel.Info,
                    Time = DateTime.UtcNow,
                    Text = string.Format("{0}\r{1}", stepInstance.Keyword + " " + stepInstance.Text, tableDescription)
                };
                Bridge.Service.AddLogItem(stepInfoRequest);
            }
        }

        public delegate void StepFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event StepFinishedHandler BeforeStepFinished;
        public static event StepFinishedHandler AfterStepFinished;
        public void TraceStepDone(BindingMatch match, object[] arguments, TimeSpan duration)
        {
            if (CurrentScenarioId != null)
            {
                var request = new FinishTestItemRequest
                {
                    Status = Status.Passed,
                    EndTime = DateTime.UtcNow
                };

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceBindingError(BindingException ex)
        {
            if (CurrentScenarioId != null)
            {
                Status = Status.Failed;

                var request = new FinishTestItemRequest
                {
                    Status = Status.Failed,
                    EndTime = DateTime.UtcNow,
                    Issue = new Issue
                    {
                        Type = IssueType.AutomationBug,
                        Comment = ex.Message
                    }
                };

                var errorRequest = new AddLogItemRequest
                {
                    TestItemId = CurrentScenarioId,
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow,
                    Text = ex.ToString()
                };
                Bridge.Service.AddLogItem(errorRequest);

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceError(Exception ex)
        {
            if (CurrentScenarioId != null)
            {
                Status = Status.Failed;

                var request = new FinishTestItemRequest
                {
                    Status = Status.Failed,
                    EndTime = DateTime.UtcNow,
                    Issue = new Issue
                    {
                        Type = IssueType.ToInvestigate,
                        Comment = ex.Message
                    }
                };

                var errorRequest = new AddLogItemRequest
                {
                    TestItemId = CurrentScenarioId,
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow,
                    Text = ex.ToString()
                };
                Bridge.Service.AddLogItem(errorRequest);

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceNoMatchingStepDefinition(StepInstance stepInstance, ProgrammingLanguage targetLanguage, System.Globalization.CultureInfo bindingCulture, List<BindingMatch> matchesWithoutScopeCheck)
        {
            if (CurrentScenarioId != null)
            {
                Status = Status.Failed;

                var errorRequest = new AddLogItemRequest
                {
                    TestItemId = CurrentScenarioId,
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow,
                    Text = "No matching step definition."
                };
                Bridge.Service.AddLogItem(errorRequest);

                var request = new FinishTestItemRequest
                {
                    Status = Status.Failed,
                    EndTime = DateTime.UtcNow,
                    Issue = new Issue
                    {
                        Type = IssueType.AutomationBug,
                        Comment = "No matching step definition."
                    }
                };

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceStepPending(BindingMatch match, object[] arguments)
        {
            if (CurrentScenarioId != null)
            {
                Status = Status.Failed;

                var errorRequest = new AddLogItemRequest
                {
                    TestItemId = CurrentScenarioId,
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow,
                    Text = "One or more step definitions are not implemented yet.\r" + match.StepBinding.Method.Name + "(" + ")"
                };
                Bridge.Service.AddLogItem(errorRequest);

                var request = new FinishTestItemRequest
                {
                    Status = Status.Failed,
                    EndTime = DateTime.UtcNow,
                    Issue = new Issue
                    {
                        Type = IssueType.ToInvestigate,
                        Comment = "Pending"
                    }
                };

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceStepSkipped()
        {
            if (CurrentScenarioId != null)
            {
                var request = new FinishTestItemRequest
                {
                    Status = Status.Skipped,
                    EndTime = DateTime.UtcNow,
                    Issue = new Issue
                    {
                        Type = IssueType.NoDefect
                    }
                };

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceWarning(string text)
        {
            if (CurrentScenarioId != null)
            {
                var request = new AddLogItemRequest
                {
                    TestItemId = CurrentScenarioId,
                    Level = LogLevel.Warning,
                    Time = DateTime.UtcNow,
                    Text = text
                };

                Bridge.Service.AddLogItem(request);
            }
        }

        public void TraceDuration(TimeSpan elapsed, string text)
        {

        }

        public void TraceDuration(TimeSpan elapsed, TechTalk.SpecFlow.Bindings.Reflection.IBindingMethod method, object[] arguments)
        {

        }
    }
}

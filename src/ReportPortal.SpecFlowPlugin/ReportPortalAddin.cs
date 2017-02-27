using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
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
        public static TestReporter CurrentFeature { get; private set; }

        public static TestReporter CurrentScenario { get; private set; }

        public static string CurrentScenarioDescription { get; private set; }

        static ReportPortalAddin()
        {
            var uri = new Uri(Configuration.ReportPortal.Server.Url);
            var project = Configuration.ReportPortal.Server.Project;
            var password = Configuration.ReportPortal.Server.Authentication.Password;

            IWebProxy proxy = null;

            if (Configuration.ReportPortal.Server.Proxy.ElementInformation.IsPresent)
            {
                proxy = new WebProxy(Configuration.ReportPortal.Server.Proxy.Server);
            }

            Bridge.Service = proxy == null
                ? new Service(uri, project, password)
                : new Service(uri, project, password, proxy);
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
                    Bridge.Context.LaunchReporter = new LaunchReporter(Bridge.Service);
                    Bridge.Context.LaunchReporter.Start(request);

                    if (AfterRunStarted != null)
                        AfterRunStarted(null,
                            new RunStartedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                }
            }
        }

        public delegate void RunFinishedHandler(object sender, RunFinishedEventArgs e);

        public static event RunFinishedHandler BeforeRunFinished;
        public static event RunFinishedHandler AfterRunFinished;

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
                if (BeforeRunFinished != null) BeforeRunFinished(null, eventArg);
                if (!eventArg.Canceled)
                {
                    Bridge.Context.LaunchReporter.Finish(request);
                    Bridge.Context.LaunchReporter.FinishTask.Wait();

                    if (AfterRunFinished != null)
                        AfterRunFinished(null,
                            new RunFinishedEventArgs(Bridge.Service, request, Bridge.Context.LaunchReporter));
                }
            }
        }

        public delegate void FeatureStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event FeatureStartedHandler BeforeFeatureStarted;
        public static event FeatureStartedHandler AfterFeatureStarted;

        [BeforeFeature(Order = -20000)]
        public static void BeforeFeature()
        {
            if (Bridge.Context.LaunchReporter != null)
            {
                var request = new StartTestItemRequest
                {
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
                    CurrentFeature = Bridge.Context.LaunchReporter.StartNewTestNode(request);
                    if (AfterFeatureStarted != null)
                        AfterFeatureStarted(null, new TestItemStartedEventArgs(Bridge.Service, request, CurrentFeature));
                }
            }
        }

        public delegate void FeatureFinishedHandler(object sender, TestItemFinishedEventArgs e);

        public static event FeatureFinishedHandler BeforeFeatureFinished;
        public static event FeatureFinishedHandler AfterFeatureFinished;

        [AfterFeature(Order = 20000)]
        public static void AfterFeature()
        {
            if (CurrentFeature != null)
            {
                var request = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = Status.Skipped
                };

                var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, CurrentFeature);
                if (BeforeFeatureFinished != null) BeforeFeatureFinished(null, eventArg);
                if (!eventArg.Canceled)
                {
                    CurrentFeature.Finish(request);
                    if (AfterFeatureFinished != null)
                        AfterFeatureFinished(null,
                            new TestItemFinishedEventArgs(Bridge.Service, request, CurrentFeature));
                }
            }
        }

        public delegate void ScenarioStartedHandler(object sender, TestItemStartedEventArgs e);

        public static event ScenarioStartedHandler BeforeScenarioStarted;
        public static event ScenarioStartedHandler AfterScenarioStarted;

        [BeforeScenario(Order = -20000)]
        public void BeforeScenario()
        {
            if (CurrentFeature != null)
            {
                CurrentScenarioDescription = string.Empty;

                Status = Status.Passed;
                var request = new StartTestItemRequest
                {
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
                    CurrentScenario = CurrentFeature.StartNewTestNode(request);

                    if (AfterScenarioStarted != null)
                        AfterScenarioStarted(this,
                            new TestItemStartedEventArgs(Bridge.Service, request, CurrentScenario));
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
            if (CurrentScenario != null)
            {
                var request = new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = Status
                };

                var eventArg = new TestItemFinishedEventArgs(Bridge.Service, request, CurrentScenario);
                if (BeforeScenarioFinished != null) BeforeScenarioFinished(this, eventArg);
                if (!eventArg.Canceled)
                {
                    CurrentScenario.Finish(request);
                    if (AfterScenarioFinished != null)
                        AfterScenarioFinished(this,
                            new TestItemFinishedEventArgs(Bridge.Service, request, CurrentScenario));

                    CurrentScenarioDescription = string.Empty;
                }
            }
        }

        public delegate void StepStartedHandler(object sender, TestItemStartedEventArgs e);
        public static event StepStartedHandler BeforeStepStarted;
        public static event StepStartedHandler AfterStepStarted;
        public void TraceStep(StepInstance stepInstance, bool showAdditionalArguments)
        {
            if (CurrentScenario != null)
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
                CurrentScenario.Update(updateScenarioRequest);

                var stepInfoRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Info,
                    //TODO log time should be greater than test time
                    Time = DateTime.UtcNow.AddMilliseconds(1),
                    Text = string.Format("{0}\r{1}", stepInstance.Keyword + " " + stepInstance.Text, tableDescription)
                };
                CurrentScenario.Log(stepInfoRequest);
            }
        }

        public delegate void StepFinishedHandler(object sender, TestItemFinishedEventArgs e);
        public static event StepFinishedHandler BeforeStepFinished;
        public static event StepFinishedHandler AfterStepFinished;
        public void TraceStepDone(BindingMatch match, object[] arguments, TimeSpan duration)
        {
            if (CurrentScenario != null)
            {
                var request = new FinishTestItemRequest
                {
                    Status = Status.Passed,
                    EndTime = DateTime.UtcNow.AddMilliseconds(1)
                };

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceBindingError(BindingException ex)
        {
            if (CurrentScenario != null)
            {
                Status = Status.Failed;

                var request = new FinishTestItemRequest
                {
                    Status = Status.Failed,
                    EndTime = DateTime.UtcNow.AddMilliseconds(1),
                    Issue = new Issue
                    {
                        Type = IssueType.AutomationBug,
                        Comment = ex.Message
                    }
                };

                var errorRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow.AddMilliseconds(1),
                    Text = ex.ToString()
                };
                CurrentScenario.Log(errorRequest);

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceError(Exception ex)
        {
            if (CurrentScenario != null)
            {
                Status = Status.Failed;

                var request = new FinishTestItemRequest
                {
                    Status = Status.Failed,
                    EndTime = DateTime.UtcNow.AddMilliseconds(1),
                    Issue = new Issue
                    {
                        Type = IssueType.ToInvestigate,
                        Comment = ex.Message
                    }
                };

                var errorRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow.AddMilliseconds(1),
                    Text = ex.ToString()
                };

                CurrentScenario.Log(errorRequest);

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceNoMatchingStepDefinition(StepInstance stepInstance, ProgrammingLanguage targetLanguage, System.Globalization.CultureInfo bindingCulture, List<BindingMatch> matchesWithoutScopeCheck)
        {
            if (CurrentScenario != null)
            {
                Status = Status.Failed;

                var errorRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow.AddMilliseconds(1),
                    Text = "No matching step definition."
                };

                CurrentScenario.Log(errorRequest);

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

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceStepPending(BindingMatch match, object[] arguments)
        {
            if (CurrentScenario != null)
            {
                Status = Status.Failed;

                var errorRequest = new AddLogItemRequest
                {
                    Level = LogLevel.Error,
                    Time = DateTime.UtcNow.AddMilliseconds(1),
                    Text = "One or more step definitions are not implemented yet.\r" + match.StepBinding.Method.Name + "(" + ")"
                };

                CurrentScenario.Log(errorRequest);

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

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceStepSkipped()
        {
            if (CurrentScenario != null)
            {
                var request = new FinishTestItemRequest
                {
                    Status = Status.Skipped,
                    EndTime = DateTime.UtcNow.AddMilliseconds(1),
                    Issue = new Issue
                    {
                        Type = IssueType.NoDefect
                    }
                };

                if (BeforeStepFinished != null) BeforeStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
                if (AfterStepFinished != null) AfterStepFinished(this, new TestItemFinishedEventArgs(Bridge.Service, request, null));
            }
        }

        public void TraceWarning(string text)
        {
            if (CurrentScenario != null)
            {
                var request = new AddLogItemRequest
                {
                    Level = LogLevel.Warning,
                    Time = DateTime.UtcNow.AddMilliseconds(1),
                    Text = text
                };

                CurrentScenario.Log(request);
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

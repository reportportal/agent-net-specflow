[![Build status](https://ci.appveyor.com/api/projects/status/k9gnrmlt3yo5gl4g?svg=true)](https://ci.appveyor.com/project/nvborisenko/agent-net-specflow)

# Installation
Install **ReportPortal.SpecFlow** NuGet package into your project with scenarios. This package depends on SpecFlow v3.

[![NuGet version](https://badge.fury.io/nu/reportportal.specflow.svg)](https://badge.fury.io/nu/reportportal.specflow)

> PS> Install-Package ReportPortal.SpecFlow

Connect plugin via your `specflow.json` file.
```json
{
  "plugins": [
    {
      "name": "ReportPortal",
      "type": "Runtime"
    }
  ],
  "stepAssemblies": [
    {
      "assembly": "ReportPortal.SpecFlowPlugin"
    }
  ]
}

```

# Configuration
Add `ReportPortal.config.json` file into tests project with `Copy to Output Directory = Copy if newer` property.

Example of config file:
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-net-specflow/master/ReportPortal.SpecFlowPlugin/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://rp.epam.com/api/v1/",
    "project": "default_project",
    "authentication": {
      "uuid": "7853c7a9-7f27-43ea-835a-cab01355fd17"
    }
  },
  "launch": {
    "name": "SpecFlow Demo Launch",
    "description": "this is description",
    "debugMode": true,
    "tags": [ "t1", "t2" ]
  }
}
```

## Combine several execution in one launch

How it can be done:
1. CI server creates a RP launch and saves the launch id to `app.config` of test binaries.
2. Test binaries are copied to VMs and run there.
3. The tests start and see that there is launch id in `app.config` and don't create a new launch - they re-use the existing one. Also they don't close it once they are done.

```c#
[BeforeTestRun(Order = -30000)]
public static void BeforeTestRunPart()
{
	ReportPortalAddin.BeforeRunStarted += ReportPortalAddin_BeforeRunStarted;
	ReportPortalAddin.BeforeRunFinished += ReportPortalAddin_BeforeRunFinished;
}

public static void ReportPortalAddin_BeforeRunStarted(object sender, RunStartedEventArgs e)
{
	var launchId = System.Configuration.ConfigurationManager.AppSettings["ReportPortalLaunchId"];
	if (launchId.IsNullOrEmpty() == false)
	{
		e.Canceled = true;
		Bridge.Context.LaunchReporter = new LaunchReporter(Bridge.Service);
		Bridge.Context.LaunchReporter.StartTask = Task.Run(() => { Bridge.Context.LaunchReporter.LaunchId = launchId; });
	}
}

public static void ReportPortalAddin_BeforeRunFinished(object sender, RunFinishedEventArgs e)
{
	var launchId = System.Configuration.ConfigurationManager.AppSettings["ReportPortalLaunchId"];
	if (launchId.IsNullOrEmpty() == false)
	{
		e.Canceled = true;
		Bridge.Context.LaunchReporter.FinishTask = Task.Run(() => { Bridge.Context.LaunchReporter.StartTask.Wait(); Bridge.Context.LaunchReporter.TestNodes.ForEach(tn => tn.FinishTask.Wait()); });
		Bridge.Context.LaunchReporter.FinishTask.Wait();
	}
}
```
4. When all tests run, CI server closes the RP launch.

## Custom handler for all requests to Report Portal

By default, the Report Portal client uses `RetryHttpClientHandler` defined in [/ReportPortal.Client/Service.cs](https://github.com/reportportal/client-net/blob/master/ReportPortal.Client/Service.cs) that will retry any failing request a few times before finally giving up.

The behavior to handle requests to Report Portal can be further customized by implementing your own `HttpMessageHandler`, subscribing to `Initializing` event and providing `Service` to the SpecFlow agent.

`RetryHttpClientHandler` from `ReportPortal.Client` library can be used as an example of how to implement `HttpMessageHandler`.

The following code defines a handler for `Initializing` event that provides `Service`:

```c#
  private static void ReportPortalAddin_Initializing(object sender, InitializingEventArgs e)
  {
    e.Service = new Service(e.Config.Server.Url, e.Config.Server.Project, e.Config.Server.Authentication.Uuid, new CustomHttpClientHandler());
  }
```

And to subscribe to `Initializing` event, add the following code in the place where you bootstrap the framework:

```c#
  ReportPortalAddin.Initializing += ReportPortalAddin_Initializing;
```

# Parallel Execution Support

Parallel Execution can be crucial if you have to run many tests in a short period of time. That is why `ReportPortal.SpecFlow` is going to support it out-of-box starting version `1.2.2-beta-12`.

It became possible after SpecFlow implemented support of Parallel Execution in version 2.0.0 and addressed the issue with `FeatureContext` injection in Before/After Feature hooks.
Please refer to [Parallel Execution](http://specflow.org/documentation/Parallel-Execution/) for more information about parallel execution in SpecFlow.

## Limitations

**At the moment only feature-level parallelization is supported.** Scenario-level parallelization might also work but it's very likely that it will create several test items per feature on Report Portal.

Some test runners (especially ones that are integrated into Visual Studio) may also create several test items per feature. The reason behind it is that these test runners execute tests individually triggering Before/After Feature hooks for each test.

### Memory (AppDomain) Isolation

In order for the functionality to work correctly, a test runner must execute all test threads in the same AppDomain. If it's not the case, each thread will create a separate launch on Report Portal.

## Unit Test Providers

Currently only NUnit v3 (`nunit`), xUnit v2 (`xunit`) and SpecFlow+ Runner (`specrun`) support running tests in parallel.

### NUnit v3

NUnit v3 supports both feature- and scenario-level parallelization although SpecFlow doesn't currently work with scenario-level parallelization (see [techtalk/SpecFlow#894](https://github.com/techtalk/SpecFlow/issues/894))

Add the following attributes to `AssemblyInfo.cs` to enable parallel execution:
```c#
[assembly: LevelOfParallelism(5)]
[assembly: Parallelizable(ParallelScope.Fixtures)]
```

### XUnit v2

XUnit v2 only supports feature-level parallelization.

Add the following attributes to `AssemblyInfo.cs` to enable parallel execution:
```c#
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = 5)]
```

### SpecRun

SpecRun is the most flexible test runner allowing you to control many aspects to test execution. SpecFlow+ Runner profiles (`.srprofile` file extension) are XML files that determine how SpecFlow+ Runner executes your tests.

Here is a sample profile file that configures SpecRun for running tests in parallel with enabled Report Portal integration:
```xml
<?xml version="1.0" encoding="utf-8"?>
<TestProfile xmlns="http://www.specflow.org/schemas/plus/TestProfile/1.5">
  <Settings projectName="SpecRun.Project.Name" />
  <Execution stopAfterFailures="0" testThreadCount="3" testSchedulingMode="Sequential" retryCount="0"/>
  <Environment testThreadIsolation="SharedAppDomain"/>
  <TestAssemblyPaths>
    <TestAssemblyPath>SpecRun.Project.Name.dll</TestAssemblyPath>
  </TestAssemblyPaths>
</TestProfile>
```
| Attribute             | Value             | Comment                                                                                                                              |
| --------------------- | ----------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `testThreadCount`     | `2` or more       | The number of tests threads used to execute tests. Turns on parallel execution.                                                      |
| `testSchedulingMode`  | `Sequential`      | Determines the order in which SpecRun executes tests. The other values may produce multiple test items per feature on Report Portal. |
| `testThreadIsolation` | `SharedAppDomain` | Determines the level of thread isolation. If the value is `AppDomain` or `Process`, each test thread will create a separate launch.  |

Refer to [SpecFlow+ Runner Profiles](http://specflow.org/plus/documentation/SpecFlowPlus-Runner-Profiles/) for more information.

### MSTest V2

At the moment MSTest V2 only supports assembly-level parallelization.

Feature- and scenario-level parallelization were recently added in MSTest V2 v1.3.0 Beta2 (see [MSTest V2: in-assembly parallel test execution](https://blogs.msdn.microsoft.com/devops/2018/01/30/mstest-v2-in-assembly-parallel-test-execution/)).

## Loggers

Any .NET logger library for Report Portal should work as long as it references ReportPortal.Shared `2.0.0-beta3` and above.
Just be sure to log messages from the main test thread. Logging messages from other threads (e.g. from code in `async` methods) may have unpredictable results.

The following code may log messages under a wrong scenario or may not log them at all. 

```c#
[Binding]
public class StepDefinitions : Steps
{
    private readonly ILog _log = LogManager.GetLogger(typeof(StepDefinitions));

    private async Task DoSomethingAsync()
    {
        await DoSomethingElseAsync();

        _log.Warn("Ad ornatus adipisci expetendis pro.");
    }
}
```

```c#
[Binding]
public class StepDefinitions : Steps
{
    private IEnumerable<string> DoSomething(List<string> names)
    {
        return names.AsParallel().Select(name =>
        {
            Bridge.LogMessage(LogLevel.Info, $"Lorem ipsum dolor sit amet {name}.");

            return DoSomethingElse(name);
        });
    }
}
```

## Thread-safe TestReporter

`ReportPortalAddin.CurrentFeature` and `ReportPortalAddin.CurrentScenario` static properties are deprecated and shouldn't be used when running tests in parallel.

To retrieve TestReporter for the current feature or for the current scenario, use `ReportPortalAddin.GetFeatureTestReporter` and `ReportPortalAddin.GetScenarioTestReporter` methods. These methods will work when called from any thread.

```c#
[Given("I have entered (.*) into the calculator")]
public void GivenIHaveEnteredSomethingIntoTheCalculator(int number)
{
    var featureTestReporter = ReportPortalAddin.GetFeatureTestReporter(this.FeatureContext);
    var scenarioTestReporter = ReportPortalAddin.GetScenarioTestReporter(this.ScenarioContext);
}
```

# Troubleshooting
All http error messages goes to `ReportPortal.log` file.

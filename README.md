[![Build status](https://ci.appveyor.com/api/projects/status/k9gnrmlt3yo5gl4g?svg=true)](https://ci.appveyor.com/project/nvborisenko/agent-net-specflow)

# Installation
Install **ReportPortal.SpecFlow** NuGet package into your project with scenarios. This package depends on SpecFlow v3.

[![NuGet version](https://badge.fury.io/nu/reportportal.specflow.svg)](https://badge.fury.io/nu/reportportal.specflow)

> PS> Install-Package ReportPortal.SpecFlow

# Configuration
Add `ReportPortal.json` file into tests project with `Copy to Output Directory = Copy if newer` property.

Example of config file:
```json
{
  "$schema": "https://raw.githubusercontent.com/reportportal/agent-net-specflow/master/src/ReportPortal.SpecFlowPlugin/ReportPortal.config.schema",
  "enabled": true,
  "server": {
    "url": "https://rp.epam.com",
    "project": "default_project",
    "apiKey": "7853c7a9-7f27-43ea-835a-cab01355fd17"
  },
  "launch": {
    "name": "SpecFlow Demo Launch",
    "description": "this is description",
    "debugMode": true,
    "attributes": [ "t1", "os:win10" ]
  }
}
```

Discover [more](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md) about configuration.

# How To

## Combine several execution in one launch

How it can be done:
1. CI server creates a RP launch and set `REPORTPORTAL_LAUNCH_ID` environment variable.
2. Execute tests as usual.
3. When all tests run, CI server finishes the RP launch.

## Custom handler for all requests to Report Portal

By default, the Report Portal client uses `RetryHttpClientHandler` defined in [/ReportPortal.Client/Service.cs](https://github.com/reportportal/client-net/blob/master/ReportPortal.Client/Service.cs) that will retry any failing request a few times before finally giving up.

The behavior to handle requests to Report Portal can be further customized by implementing your own `HttpMessageHandler`, subscribing to `Initializing` event and providing `Service` to the SpecFlow agent.

`RetryHttpClientHandler` from `ReportPortal.Client` library can be used as an example of how to implement `HttpMessageHandler`.

The following code defines a handler for `Initializing` event that provides `Service`:

```c#
  private static void ReportPortalAddin_Initializing(object sender, InitializingEventArgs e)
  {
		e.Service = new Service(
			new Uri(e.Config.GetValue<string>(ConfigurationPath.ServerUrl)),
			e.Config.GetValue<string>(ConfigurationPath.ServerProject),
			e.Config.GetValue<string>(ConfigurationPath.ServerAuthenticationUuid),
			new CustomHttpClientHandler());
  }
```

And to subscribe to `Initializing` event, add the following code in the place where you bootstrap the framework:

```c#
  ReportPortalAddin.Initializing += ReportPortalAddin_Initializing;
```

## Override config via Environment Variables

```cmd
set reportportal_launch_name="My new launch name"
# execute tests
```

`reportportal_` prefix is used for naming variables, and `_` is used as delimeter. For example to override `Server.Authentication.Uuid` parameter, we need specify `ReportPortal_Server_Authentication_Uuid` in environment variables. To override launch tags we need specify `ReportPortal_Launch_Attributes` with `tag1;os:linux` value (`;` used as separator for list of values).

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
All http error messages goes to `ReportPortal.*.log` file.

# Integrate logger framework
- [NLog](https://github.com/reportportal/logger-net-nlog)
- [log4net](https://github.com/reportportal/logger-net-log4net)
- [Serilog](https://github.com/reportportal/logger-net-serilog)
- [System.Diagnostics.TraceListener](https://github.com/reportportal/logger-net-tracelistener)

And [how](https://github.com/reportportal/commons-net/blob/master/docs/Logging.md) you can improve your logging experience with attachments or nested steps.


# Useful extensions
- [SourceBack](https://github.com/nvborisenko/reportportal-extensions-sourceback) adds piece of test code where test was failed
- [Insider](https://github.com/nvborisenko/reportportal-extensions-insider) brings more reporting capabilities without coding like methods invocation as nested steps


# License
ReportPortal is licensed under [Apache 2.0](https://github.com/reportportal/agent-net-specflow/blob/master/LICENSE)

We use Google Analytics for sending anonymous usage information as library's name/version and the agent's name/version when starting launch. This information might help us to improve integration with ReportPortal. Used by the ReportPortal team only and not for sharing with 3rd parties. You are able to [turn off](https://github.com/reportportal/commons-net/blob/master/docs/Configuration.md#analytics) it if needed.

[![Build status](https://ci.appveyor.com/api/projects/status/k9gnrmlt3yo5gl4g?svg=true)](https://ci.appveyor.com/project/nvborisenko/agent-net-specflow)

# Installation
Install **ReportPortal.SpecFlow** NuGet package into your project with scenarios.

[![NuGet version](https://badge.fury.io/nu/reportportal.specflow.svg)](https://badge.fury.io/nu/reportportal.specflow)

> PS> Install-Package ReportPortal.SpecFlow

After installing NuGet package your App.config is modified. Report Portal plugin and step assembly will be registered in the specFlow section.
```xml
<specFlow>
  ...
  <plugins>
    <add name="ReportPortal" type="Runtime" />
  </plugins>
  ...
  <stepAssemblies>
    <stepAssembly assembly="ReportPortal.SpecFlowPlugin" />
  </stepAssemblies>
  ...
</specFlow>
```

# Configuration
All settings are stored in *ReportPortal.SpecFlowPlugin.dll.config* file which was added into your project by nuget installation.

|Property |Description|
|-------- |-----------|
|enabled |Enable/Disable reporting to Report Portal server|
|server - url |The base URI to Report Portal REST web service|
|server - project |Name of the project|
|authentication - username |Name of the user|
|authentication - password |Password of the user. UID can be used instead of opened password. You can find it on user's profile page|
|launch - debugMode |Turn on/off debugging of your tests. Only you have access for test results if test execution is proceeded in debug mode|
|launch - name |Name of test execution|
|launch - tags |Comma separated tags for test execution|

Example of config file:
```xml
<configuration>
  <configSections>
    <section name="reportPortal" type="ReportPortal.SpecFlowPlugin.ReportPortalSection, ReportPortal.SpecFlowPlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
  </configSections>
  <reportPortal enabled="true">
    <server url="https://{SERVER}:{PORT}/api/v1/" project="default_project">
      <authentication username="default" password="aa19555c-c9ce-42eb-bb11-87757225d535" />
    </server>
    <launch name="SpecFlow Demo Launch" debugMode="true" tags="t1,t2" />
  </reportPortal>
</configuration>
```

Alternatively,you could configure the plugin via app.config of your application, instead of *ReportPortal.SpecFlowPlugin.dll.config*.Just add above mentioned config 
options to app.config and remove *ReportPortal.SpecFlowPlugin.dll.config*.


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
		Bridge.Context.LaunchReporter.FinishTask = Task.Run(() => { StartTask.Wait(); TestNodes.ForEach(tn => tn.FinishTask.Wait()); });
	}
}
```
4. When all tests run, CI server closes the RP launch.

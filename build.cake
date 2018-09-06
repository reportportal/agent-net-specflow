#tool nuget:?package=NUnit.ConsoleRunner&version=3.5.0
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");
var build = Argument("build", "1.0.0");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var isAppVeyorBuild = AppVeyor.IsRunningOnAppVeyor;

// Define directories.
var buildDir = Directory("./src/ReportPortal.SpecFlowPlugin/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does(() =>
{
	CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
	.IsDependentOn("Clean")
	.Does(() =>
{
	NuGetRestore("./src/ReportPortal.SpecFlowPlugin.sln");
});

Task("Build")
	.IsDependentOn("Restore-NuGet-Packages")
	.Does(() =>
{
	if(IsRunningOnWindows())
	{
	  // Use MSBuild
	  MSBuild("./src/ReportPortal.SpecFlowPlugin.sln", new MSBuildSettings().SetConfiguration(configuration));
	}
	else
	{
	  // Use XBuild
	  XBuild("./src/ReportPortal.SpecFlowPlugin.sln", settings =>
		settings.SetConfiguration(configuration));
	}
});

Task("Package")
	.IsDependentOn("Build")
	.Does(() =>
{
	if (isAppVeyorBuild)
	{
		if (AppVeyor.Environment.Repository.Tag.IsTag)
		{
			build = AppVeyor.Environment.Repository.Tag.Name;
			BuildSystem.AppVeyor.UpdateBuildVersion(build);
		}
		else
		{
			build = AppVeyor.Environment.Build.Version;
		}
	}
	else
	{
		build += "-local";
	}
	NuGetPack("src/ReportPortal.SpecFlowPlugin/ReportPortal.SpecFlowPlugin.nuspec", new NuGetPackSettings()
	{
		BasePath = "./src/ReportPortal.SpecFlowPlugin/bin/" + configuration,
		Version = build
	});
	}
	);

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
	.IsDependentOn("Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

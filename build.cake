//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDirs = new List<string>()
{
    Directory("./Source/Breeze.NHibernate/bin") + Directory(configuration),
    Directory("./Source/Breeze.NHibernate.AspNetCore.Mvc/bin") + Directory(configuration),
    Directory("./Source/Breeze.NHibernate.Tests/bin") + Directory(configuration)
};

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("clean")
    .Does(() =>
{
    foreach(var buildDir in buildDirs)
    {
        CleanDirectory(buildDir);
    }
});

Task("restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("./Source/Breeze.NHibernate.sln");
});

Task("build")
    .IsDependentOn("restore")
    .Does(() =>
{
    DotNetCoreBuild("./Source/Breeze.NHibernate.sln", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore"),
    });
});

Task("test")
    .IsDependentOn("build")
    .Does(() =>
{
    DotNetCoreTest("./Source/Breeze.NHibernate.Tests/Breeze.NHibernate.Tests.csproj", new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
        Filter = "FullyQualifiedName!=Breeze.NHibernate.Tests.NorthwindIBTests.TestModule"
    });
});

Task("test-northwind")
    .IsDependentOn("build")
    .Does(() =>
{
    using(var process = StartAndReturnProcess("Source/Breeze.NHibernate.NorthwindIB.Tests/bin/Release/netcoreapp3.1/Breeze.NHibernate.NorthwindIB.Tests.exe"))
    {
        try
        {
            DotNetCoreTest("./Source/Breeze.NHibernate.Tests/Breeze.NHibernate.Tests.csproj", new DotNetCoreTestSettings
            {
                Configuration = configuration,
                NoBuild = true,
                Filter = "FullyQualifiedName=Breeze.NHibernate.Tests.NorthwindIBTests.TestModule"
            });
        }
        finally
        {
            process.Kill();
        }
    }
});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("clean-packages")
    .Does(() =>
{
    CleanDirectory(PACKAGE_DIR);
});

Task("pack")
    .IsDependentOn("clean-packages")
    .Description("Creates NuGet packages")
    .Does(() =>
{
    CreateDirectory(PACKAGE_DIR);

    var projects = new string[]
    {
        "Source/Breeze.NHibernate/Breeze.NHibernate.csproj",
        "Source/Breeze.NHibernate.AspNetCore.Mvc/Breeze.NHibernate.AspNetCore.Mvc.csproj",
    };

    foreach(var project in projects)
    {
        MSBuild(project, new MSBuildSettings {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append("/t:pack")
                .Append("/p:PackageOutputPath=\"" + PACKAGE_DIR + "\"")
        });
    }
});

Task("async")
    .IsDependentOn("restore")
    .Does(() =>
{
    DotNetCoreTool("async-generator");
});
    
Task("publish")
    .IsDependentOn("pack")
    .Does(() =>
{
    foreach(var package in System.IO.Directory.GetFiles(PACKAGE_DIR, "*.nupkg").Where(o => !o.Contains("symbols")))
    {
        NuGetPush(package, new NuGetPushSettings()
        {
            Source = "https://api.nuget.org/v3/index.json"
        });
    }
});



//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("default")
    .IsDependentOn("test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);

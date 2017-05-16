///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Polly&version=5.1.0"
#addin "nuget:?package=NuGet.Core&version=2.14.0"

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

#tool "nuget:?package=xunit.runner.console&version=2.2.0"

///////////////////////////////////////////////////////////////////////////////
// USINGS
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using Polly;
using NuGet;

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var platform = Argument("platform", "Any CPU");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// CONFIGURATION
///////////////////////////////////////////////////////////////////////////////

var MainRepo = "wieslawsoltes/SimpleWavSplitter";
var MasterBranch = "master";
var AssemblyInfoPath = File("./src/Shared/SharedAssemblyInfo.cs");
var ReleasePlatform = "Any CPU";
var ReleaseConfiguration = "Release";
var MSBuildSolution = "./SimpleWavSplitter.sln";
var XBuildSolution = "./SimpleWavSplitter.sln";

///////////////////////////////////////////////////////////////////////////////
// PARAMETERS
///////////////////////////////////////////////////////////////////////////////

var isPlatformAnyCPU = StringComparer.OrdinalIgnoreCase.Equals(platform, "Any CPU");
var isPlatformX86 = StringComparer.OrdinalIgnoreCase.Equals(platform, "x86");
var isPlatformX64 = StringComparer.OrdinalIgnoreCase.Equals(platform, "x64");
var isLocalBuild = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();
var isRunningOnAppVeyor = BuildSystem.AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = BuildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;
var isMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, BuildSystem.AppVeyor.Environment.Repository.Name);
var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch, BuildSystem.AppVeyor.Environment.Repository.Branch);
var isTagged = BuildSystem.AppVeyor.Environment.Repository.Tag.IsTag 
               && !string.IsNullOrWhiteSpace(BuildSystem.AppVeyor.Environment.Repository.Tag.Name);
var isReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleasePlatform, platform) 
                   && StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, configuration);
var isMyGetRelease = !isTagged && isReleasable;
var isNuGetRelease = isTagged && isReleasable;

///////////////////////////////////////////////////////////////////////////////
// VERSION
///////////////////////////////////////////////////////////////////////////////

var version = ParseAssemblyInfo(AssemblyInfoPath).AssemblyVersion;

if (isRunningOnAppVeyor)
{
    if (isTagged)
    {
        // Use Tag Name as version
        version = BuildSystem.AppVeyor.Environment.Repository.Tag.Name;
    }
    else
    {
        // Use AssemblyVersion with Build as version
        version += "-build" + EnvironmentVariable("APPVEYOR_BUILD_NUMBER") + "-alpha";
    }
}

///////////////////////////////////////////////////////////////////////////////
// DIRECTORIES
///////////////////////////////////////////////////////////////////////////////

var artifactsDir = (DirectoryPath)Directory("./artifacts");
var testResultsDir = artifactsDir.Combine("test-results");
var nugetRoot = artifactsDir.Combine("nuget");
var chocolateyRoot = artifactsDir.Combine("chocolatey");
var zipRoot = artifactsDir.Combine("zip");
var dirSuffix = isPlatformAnyCPU ? configuration : platform + "/" + configuration;
var buildDirs = 
    GetDirectories("./src/**/bin/" + dirSuffix) + 
    GetDirectories("./src/**/obj/" + dirSuffix) + 
    GetDirectories("./samples/**/bin/" + dirSuffix) + 
    GetDirectories("./samples/**/obj/" + dirSuffix) +
    GetDirectories("./tests/**/bin/" + dirSuffix) + 
    GetDirectories("./tests/**/obj/" + dirSuffix);

var fileZipSuffix = isPlatformAnyCPU ? configuration + "-" + version + ".zip" : platform + "-" + configuration + "-" + version + ".zip";

var zipSourceAvaloniaDirs = (DirectoryPath)Directory("./src/SimpleWavSplitter.Avalonia/bin/" + dirSuffix);
var zipSourceWpfDirs = (DirectoryPath)Directory("./src/SimpleWavSplitter.Wpf/bin/" + dirSuffix);
var zipSourceConsoleDirs = (DirectoryPath)Directory("./src/SimpleWavSplitter.Console/bin/" + dirSuffix);

var zipTargetAvaloniaDirs = zipRoot.CombineWithFilePath("SimpleWavSplitter.Avalonia-" + fileZipSuffix);
var zipTargetWpfDirs = zipRoot.CombineWithFilePath("SimpleWavSplitter.Wpf-" + fileZipSuffix);
var zipTargetConsoleDirs = zipRoot.CombineWithFilePath("SimpleWavSplitter.Console-" + fileZipSuffix);

///////////////////////////////////////////////////////////////////////////////
// NUGET NUSPECS
///////////////////////////////////////////////////////////////////////////////

var nuspecNuGetWavFile = new NuGetPackSettings()
{
    Id = "WavFile",
    Version = version,
    Authors = new [] { "wieslaw.soltes" },
    Owners = new [] { "wieslaw.soltes" },
    LicenseUrl = new Uri("http://opensource.org/licenses/MIT"),
    ProjectUrl = new Uri("https://github.com/wieslawsoltes/SimpleWavSplitter/"),
    RequireLicenseAcceptance = false,
    Symbols = false,
    NoPackageAnalysis = true,
    Description = "Split multi-channel WAV files into single channel WAV files.",
    Copyright = "Copyright 2016",
    Tags = new [] { "Wav", "Audio", "Splitter", "Multi-channel", "Managed", "C#" },
    Files = new []
    {
        // netstandard1.3
        new NuSpecContent { Source = "src/WavFile/bin/" + dirSuffix + "/netstandard1.3/" + "WavFile.dll", Target = "lib/netstandard1.3" },
        new NuSpecContent { Source = "src/WavFile/bin/" + dirSuffix + "/netstandard1.3/" + "WavFile.xml", Target = "lib/netstandard1.3" },
        // net45
        new NuSpecContent { Source = "src/WavFile/bin/" + dirSuffix + "/net45/" + "WavFile.dll", Target = "lib/net45" },
        new NuSpecContent { Source = "src/WavFile/bin/" + dirSuffix + "/net45/" + "WavFile.xml", Target = "lib/net45" }
    },
    BasePath = Directory("./"),
    OutputDirectory = nugetRoot
};

var nuspecNuGetSettings = new List<NuGetPackSettings>();

nuspecNuGetSettings.Add(nuspecNuGetWavFile);

var nugetPackages = nuspecNuGetSettings.Select(nuspec => {
    return nuspec.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Id, ".", nuspec.Version, ".nupkg"));
}).ToArray();

///////////////////////////////////////////////////////////////////////////////
// CHOCOLATEY NUSPECS
///////////////////////////////////////////////////////////////////////////////

var SetChocolateyNuspecCommonProperties = new Action<ChocolateyPackSettings> ((nuspec) => {
    nuspec.Version = version;
    nuspec.Authors = new [] { "wieslaw.soltes" };
    nuspec.Owners = new [] { "wieslaw.soltes" };
    nuspec.LicenseUrl = new Uri("http://opensource.org/licenses/MIT");
    nuspec.ProjectUrl = new Uri("https://github.com/wieslawsoltes/SimpleWavSplitter/");
    nuspec.PackageSourceUrl = new Uri("https://github.com/wieslawsoltes/SimpleWavSplitter/");
    nuspec.ProjectSourceUrl = new Uri("https://github.com/wieslawsoltes/SimpleWavSplitter/");
    nuspec.BugTrackerUrl = new Uri("https://github.com/wieslawsoltes/SimpleWavSplitter/issues/");
    nuspec.DocsUrl = new Uri("http://wieslawsoltes.github.io/SimpleWavSplitter/");
    nuspec.RequireLicenseAcceptance = false;
    nuspec.Description = "Split multi-channel WAV files into single channel WAV files.";
    nuspec.Copyright = "Copyright 2016";
    nuspec.Tags = new [] { "Wav", "Audio", "Splitter", "Multi-channel", "Managed", "C#" };
});

Func<DirectoryPath, ChocolateyNuSpecContent[]> GetChocolateyNuSpecContent = path => {
    var files = GetFiles(path.FullPath + "/*.dll") + GetFiles(path.FullPath + "/*.exe");
    return files.Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = "bin" }).ToArray();
};

var nuspecChocolateySettings = new Dictionary<ChocolateyPackSettings, DirectoryPath>();

///////////////////////////////////////////////////////////////////////////////
// src: SimpleWavSplitter.Avalonia
///////////////////////////////////////////////////////////////////////////////
nuspecChocolateySettings.Add(
    new ChocolateyPackSettings
    {
        Id = "SimpleWavSplitter-Avalonia",
        Title = "SimpleWavSplitter (Avalonia)",
        OutputDirectory = chocolateyRoot
    },
    zipSourceAvaloniaDirs);

///////////////////////////////////////////////////////////////////////////////
// src: SimpleWavSplitter.Wpf
///////////////////////////////////////////////////////////////////////////////
nuspecChocolateySettings.Add(
    new ChocolateyPackSettings
    {
        Id = "SimpleWavSplitter-Wpf",
        Title = "SimpleWavSplitter (WPF)",
        OutputDirectory = chocolateyRoot
    },
    zipSourceWpfDirs);

nuspecChocolateySettings.ToList().ForEach((nuspec) => SetChocolateyNuspecCommonProperties(nuspec.Key));

var chocolateyPackages = nuspecChocolateySettings.Select(nuspec => {
    return nuspec.Key.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Key.Id, ".", nuspec.Key.Version, ".nupkg"));
}).ToArray();

///////////////////////////////////////////////////////////////////////////////
// INFORMATION
///////////////////////////////////////////////////////////////////////////////

Information("Building version {0} of SimpleWavSplitter ({1}, {2}, {3}) using version {4} of Cake.", 
    version,
    platform,
    configuration,
    target,
    typeof(ICakeContext).Assembly.GetName().Version.ToString());

if (isRunningOnAppVeyor)
{
    Information("Repository Name: " + BuildSystem.AppVeyor.Environment.Repository.Name);
    Information("Repository Branch: " + BuildSystem.AppVeyor.Environment.Repository.Branch);
}

Information("Target: " + target);
Information("Platform: " + platform);
Information("Configuration: " + configuration);
Information("IsLocalBuild: " + isLocalBuild);
Information("IsRunningOnUnix: " + isRunningOnUnix);
Information("IsRunningOnWindows: " + isRunningOnWindows);
Information("IsRunningOnAppVeyor: " + isRunningOnAppVeyor);
Information("IsPullRequest: " + isPullRequest);
Information("IsMainRepo: " + isMainRepo);
Information("IsMasterBranch: " + isMasterBranch);
Information("IsTagged: " + isTagged);
Information("IsReleasable: " + isReleasable);
Information("IsMyGetRelease: " + isMyGetRelease);
Information("IsNuGetRelease: " + isNuGetRelease);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories(buildDirs);
    CleanDirectory(artifactsDir);
    CleanDirectory(testResultsDir);
    CleanDirectory(nugetRoot);
    CleanDirectory(chocolateyRoot);
    CleanDirectory(zipRoot);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var maxRetryCount = 5;
    var toolTimeout = 1d;
    Policy
        .Handle<Exception>()
        .Retry(maxRetryCount, (exception, retryCount, context) => {
            if (retryCount == maxRetryCount)
            {
                throw exception;
            }
            else
            {
                Verbose("{0}", exception);
                toolTimeout+=0.5;
            }})
        .Execute(()=> {
            if(isRunningOnWindows)
            {
                NuGetRestore(MSBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
            }
            else
            {
                NuGetRestore(XBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
            }
        });
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(isRunningOnWindows)
    {
        MSBuild(MSBuildSolution, settings => {
            settings.SetConfiguration(configuration);
            settings.WithProperty("Platform", "\"" + platform + "\"");
            settings.SetVerbosity(Verbosity.Minimal);
        });
    }
    else
    {
        XBuild(XBuildSolution, settings => {
            settings.SetConfiguration(configuration);
            settings.WithProperty("Platform", "\"" + platform + "\"");
            settings.SetVerbosity(Verbosity.Minimal);
        });
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    string pattern = "./tests/**/bin/" + dirSuffix + "/*.UnitTests.dll";

    if (isPlatformAnyCPU || isPlatformX86)
    {
        XUnit2(pattern, new XUnit2Settings { 
            ToolPath = "./tools/xunit.runner.console/tools/xunit.console.x86.exe",
            OutputDirectory = testResultsDir,
            XmlReportV1 = true,
            NoAppDomain = true
        });
    }
    else
    {
        XUnit2(pattern, new XUnit2Settings { 
            ToolPath = "./tools/xunit.runner.console/tools/xunit.console.exe",
            OutputDirectory = testResultsDir,
            XmlReportV1 = true,
            NoAppDomain = true
        });
    }
});

Task("Zip-Files")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    Zip(zipSourceAvaloniaDirs, 
        zipTargetAvaloniaDirs, 
        GetFiles(zipSourceAvaloniaDirs.FullPath + "/*.dll") + 
        GetFiles(zipSourceAvaloniaDirs.FullPath + "/*.exe"));

    if (isRunningOnWindows)
    {
        Zip(zipSourceWpfDirs, 
            zipTargetWpfDirs, 
            GetFiles(zipSourceWpfDirs.FullPath + "/*.dll") + 
            GetFiles(zipSourceWpfDirs.FullPath + "/*.exe"));
    }

    Zip(zipSourceConsoleDirs, 
        zipTargetConsoleDirs, 
        GetFiles(zipSourceConsoleDirs.FullPath + "/*.dll") + 
        GetFiles(zipSourceConsoleDirs.FullPath + "/*.exe"));
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    foreach(var nuspec in nuspecNuGetSettings)
    {
        NuGetPack(nuspec);
    }
});

Task("Create-Chocolatey-Packages")
    .IsDependentOn("Run-Unit-Tests")
    .WithCriteria(() => isRunningOnWindows)
    .Does(() =>
{
    foreach(var nuspec in nuspecChocolateySettings)
    {
        nuspec.Key.Files = GetChocolateyNuSpecContent(nuspec.Value);
        ChocolateyPack(nuspec.Key);
    }
});

Task("Publish-MyGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isMyGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("MYGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve MyGet API key.");
    }

    var apiUrl = EnvironmentVariable("MYGET_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve MyGet API url.");
    }

    foreach(var nupkg in nugetPackages)
    {
        NuGetPush(nupkg, new NuGetPushSettings {
            Source = apiUrl,
            ApiKey = apiKey
        });
    }
})
.OnError(exception =>
{
    Information("Publish-MyGet Task failed, but continuing with next Task...");
});

Task("Publish-NuGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isNuGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("NUGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve NuGet API key.");
    }

    var apiUrl = EnvironmentVariable("NUGET_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve NuGet API url.");
    }

    foreach(var nupkg in nugetPackages)
    {
        NuGetPush(nupkg, new NuGetPushSettings {
            ApiKey = apiKey,
            Source = apiUrl
        });
    }
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
});

Task("Publish-Chocolatey")
    .IsDependentOn("Create-Chocolatey-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isNuGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("CHOCOLATEY_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve Chocolatey API key.");
    }

    var apiUrl = EnvironmentVariable("CHOCOLATEY_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve Chocolatey API url.");
    }

    foreach(var nupkg in chocolateyPackages)
    {
        ChocolateyPush(nupkg, new ChocolateyPushSettings {
            ApiKey = apiKey,
            Source = apiUrl
        });
    }
})
.OnError(exception =>
{
    Information("Publish-Chocolatey Task failed, but continuing with next Task...");
});

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
  .IsDependentOn("Zip-Files")
  .IsDependentOn("Create-NuGet-Packages")
  .IsDependentOn("Create-Chocolatey-Packages");

Task("Default")
  .IsDependentOn("Package");

Task("AppVeyor")
  .IsDependentOn("Zip-Files")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet")
  .IsDependentOn("Publish-Chocolatey");

Task("Travis")
  .IsDependentOn("Run-Unit-Tests");

///////////////////////////////////////////////////////////////////////////////
// EXECUTE
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);

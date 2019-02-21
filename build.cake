#addin nuget:?package=Optional&version=4.0.0

using Optional;

Option<string> NuGetPackageVersion = Argument<string>("nugetPackageVersion", null).SomeNotNull();
Option<string> NuGetApiKey => (Argument<string>("nugetApiKey", null) ?? System.Environment.GetEnvironmentVariable("BC_OPTIONAL_NUGET_APIKEY")).SomeNotNull();

var target = Argument("target", "Default");

var sln = File("./src/Optional.sln");

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Restore")
    .Does(() =>
    {
        NuGetRestore(sln);
    });

Task("Build")
    .Does(() =>
    {
        MSBuild(sln, new MSBuildSettings 
        {
            Verbosity = Verbosity.Minimal,
            ToolVersion = MSBuildToolVersion.VS2017,
            Configuration = "Release",
            PlatformTarget = PlatformTarget.MSIL
        });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        MSTest("./src/Optional.Tests/bin/release/**/Optional.Tests.dll");
    });

Task("CleanNuGetPackages")
    .Does(()=>{
        DeleteFiles("./nuget/**/*.nupkg");
        Information("Deleted all old NuGet package files.");
    });

Task("Pack")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("CleanNuGetPackages")
    .Does(() =>    
        NuGetPackageVersion.Match(
            some: version => Pack("Optional", version, new [] { "net35", "net45", "netstandard1.0", "netstandard2.0" }),
            none: () => throw new Exception("Required argument with Nuget Package Version was not provided.")));

Task("PublishToNuget")
    .IsDependentOn("Pack")
    .Does(() => NuGetApiKey.Match(
        none: () => throw new Exception("NuGet API was not provided in any way."),
        some: apiKey => {
            var nupkgFiles = GetFiles("nuget/**/*.nupkg");
            foreach(var nupkg in nupkgFiles) {
                try {
                    PushToNuget(nupkg, apiKey);
                } catch(System.Exception) {
                    // swallow 
                }
            }
        }));

RunTarget(target);

void Pack(string projectName, string version, string[] targets) 
{
    var nuGetPackSettings   = new NuGetPackSettings 
    {
        Version = version, 
        NoPackageAnalysis = true,
        BasePath = "./src/" + projectName + "/bin/release",
        OutputDirectory = "./nuget/" + projectName,
        Files = targets
            .SelectMany(target => new []
            {
                new NuSpecContent { Source = target + "/" + projectName + ".dll", Target = "lib/" + target },
                new NuSpecContent { Source = target + "/" + projectName + ".xml", Target = "lib/" + target }
            })
            .ToArray()
    };

    NuGetPack("./nuget/" + projectName + ".nuspec", nuGetPackSettings);
}

void PushToNuget(FilePath package, string apiKey) {
    var settings = new NuGetPushSettings {
        Source = "https://api.nuget.org/v3/index.json",
        ApiKey = apiKey
    };
    NuGetPush(package, settings);
}
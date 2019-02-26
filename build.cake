#addin nuget:?package=BC.Optional&version=1.0.0

using Optional;

Option<string> NuGetPackageVersion = Argument<string>("nugetPackageVersion", null).SomeNotNull();
Option<string> NuGetApiKey => (Argument<string>("nugetApiKey", null) ?? System.Environment.GetEnvironmentVariable("BC_OPTIONAL_NUGET_APIKEY")).SomeNotNull();

var target = Argument("target", "Default");

var sln = File("./src/Optional.sln");

Task("Default")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTest");

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

Task("UnitTest")
    .IsDependentOn("Build")
    .Does(RunUnitTests);


Task("CleanNuGetPackages")
    .Does(()=>{
        DeleteFiles("./nuget/**/*.nupkg");
        Information("Deleted all old NuGet package files.");
    });

Task("PackNuget")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTest")
    .IsDependentOn("CleanNuGetPackages")
    .Does(PackNuget);

Task("PublishToNuget")
    .IsDependentOn("PackNuget")
    .Does(PublishToNuget);

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

void RunUnitTests(){
    MSTest("./src/Optional.Tests/bin/release/**/Optional.Tests.dll");
}

void PackNuget() {
    NuGetPackageVersion.Match(
            some: version => Pack("Optional", version, new [] { "net35", "net45", "netstandard1.0", "netstandard2.0" }),
            none: () => throw new Exception("Required argument with Nuget Package Version was not provided."));
}

void PublishToNuget() {
    NuGetApiKey.Match(
        none: () => throw new Exception("NuGet API was not provided in any way."),
        some: apiKey => {
            var nupkgFiles = GetFiles("nuget/**/*.nupkg");
            foreach(var nupkg in nupkgFiles) {
                PushToNuget(nupkg, apiKey);
            }
        });
}

void PushToNuget(FilePath package, string apiKey) {
    var settings = new NuGetPushSettings {
        Source = "https://api.nuget.org/v3/index.json",
        ApiKey = apiKey
    };
    NuGetPush(package, settings);
}
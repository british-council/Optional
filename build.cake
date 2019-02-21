#addin nuget:?package=Optional&version=4.0.0

using Optional;

Option<string> nugetPackageVersion = Argument<string>("nugetPackageVersion", null).SomeNotNull();

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

Task("Pack")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .Does(() =>
    {
        nugetPackageVersion.Match(
            some: version => Pack("Optional", version, new [] { "net35", "net45", "netstandard1.0", "netstandard2.0" }),
            none: () => throw new Exception("Required argument with Nuget Package Version was not provided.")
        );
    });
    
RunTarget(target);

public void Pack(string projectName, string version, string[] targets) 
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
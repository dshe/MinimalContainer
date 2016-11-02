#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"

var target = Argument("target", "Default");
var solution = File("src/StandardContainer.sln");
var sourceFile = File("src/StandardContainer/StandardContainer.cs");
var assemblyInfoFile = File("src/AssemblyInfo.cs");
var gitVersion = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

// determine Git version, patch AssemblyInfo.cs, update source file header
Task("Version").Does(() =>
{
	Information("GitVersion: " + gitVersion.FullSemVer);

    GitVersion(new GitVersionSettings 
	{
        UpdateAssemblyInfo = true,
        OutputType = GitVersionOutput.BuildServer,
		UpdateAssemblyInfoFilePath = assemblyInfoFile
    });

	var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile);
	Information("Writing AssemblyInfo.cs version: " + assemblyInfo.AssemblyVersion);

	var header = 
		"// StandardContainer.cs " + assemblyInfo.AssemblyVersion + System.Environment.NewLine +
		"// " + assemblyInfo.Copyright + " " + assemblyInfo.Company + System.Environment.NewLine +
		"// License: http://www.apache.org/licenses/LICENSE-2.0" + System.Environment.NewLine +
		System.Environment.NewLine;

	string txt = System.IO.File.ReadAllText(sourceFile);
	txt = header + txt.Substring(txt.IndexOf("using"));
	System.IO.File.WriteAllText(sourceFile, txt);
});

Task("Build").IsDependentOn("Version").Does(() =>
{
	NuGetRestore(solution);

	CleanDirectories("src/**/**/Release");
	CleanDirectories("src/**/**/Debug");

	DotNetBuild(solution, settings => settings.WithTarget("Clean,Rebuild")
		.SetConfiguration("Release")
		.WithProperty("TreatWarningsAsErrors","true")
		.SetVerbosity(Cake.Core.Diagnostics.Verbosity.Minimal));
	//tag = AppVeyor.Environment.Repository.Tag.Name;
	//AppVeyor.UpdateBuildVersion(tag);
});

Task("Test").IsDependentOn("Build").Does(() =>
{
	var settings = new XUnit2Settings();
	settings.TraitsToExclude.Add("Category", new List<string> { "Performance"});
	XUnit2("src/StandardContainer.Tests/bin/Release/StandardContainer.Tests.dll", settings);
});

Task("CreateNuGetPackage").IsDependentOn("Test").Does(() =>
{
	string txt = System.IO.File.ReadAllText(sourceFile);
	txt = txt.Replace("namespace StandardContainer", "namespace $rootnamespace$.StandardContainer");
	System.IO.File.WriteAllText("StandardContainer.cs.pp", txt.NormalizeLineEndings());

	var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile);

	var settings = new NuGetPackSettings {
            Id                      = assemblyInfo.Product + ".Source",
            Title                   = assemblyInfo.Title + " Source",
            Version                 = gitVersion.NuGetVersion,
            Authors                 = new[] {assemblyInfo.Company},
            Owners                  = new[] {assemblyInfo.Company},
            Summary                 = assemblyInfo.Description,
            Description             = assemblyInfo.Description,
            ProjectUrl              = new Uri("https://github.com/dshe/StandardContainer"),
            IconUrl                 = new Uri("https://raw.githubusercontent.com/dshe/StandardContainer/master/worm64.png"),
            LicenseUrl              = new Uri("http://www.apache.org/licenses/LICENSE-2.0"),
            Copyright               = assemblyInfo.Copyright,
            //ReleaseNotes            = new [] {"ReleaseNotes"},
            Tags                    = new [] {"IoC", "container", "dependency injection", "inversion of control", "netstandard1.0", "portable"},
            RequireLicenseAcceptance= false,
            Symbols                 = false,
            NoPackageAnalysis       = true,
            Files                   = new [] { new NuSpecContent { Source = "StandardContainer.cs.pp", Target = "content/StandardContainer/"},},
            //BasePath                = "../StandardContainer",
            OutputDirectory         = "."
    };
    NuGetPack(settings); 
});


Task("PushNuGetPackage").IsDependentOn("CreateNuGetPackage").Does(() =>
{
    var package = "../CakeTutorial.1.0.1.nupkg";

    NuGetPush(package, new NuGetPushSettings {
        Source = "https://nuget.org/",
        //ApiKey = "NugetApiKey"
		ApiKey = EnvironmentVariable("NuGet_API_KEY")
    });
});


Task("Default").IsDependentOn("CreateNuGetPackage");

RunTarget(target);

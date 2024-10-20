// add package directives
#tool "nuget:?package=ReportGenerator&version=5.1.11"
#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#r "Spectre.Console"

// import spectre for colored console output
using Spectre.Console

var target = Argument("target", "Default");
var configuration = Argument("Configuration", "Release");
var solution = "./DreamBot.sln";
var testResultsDir = Argument("testResultsDir", "./TestResults");

Task("Restore")
    .Does(() =>
    {
        DotNetRestore();

    });
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        DotNetBuild(solution,
           new DotNetBuildSettings()
                {
                    Configuration = configuration
                });
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {	
        // TODO: remove it after issue be solved: https://github.com/dotnet/sdk/issues/29543
		System.Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US");
		
		CleanDirectory(testResultsDir);
	
		var testSettings = new DotNetTestSettings 
		{
			NoBuild = true,
			Verbosity = DotNetVerbosity.Normal,
			Configuration = configuration,
			TestAdapterPath = ".",
			ResultsDirectory = Directory(testResultsDir),
			ArgumentCustomization = args => args			
				.Append("--collect")
				.AppendQuoted("XPlat Code Coverage")
				.Append("--logger")
				.Append("trx")
		};
		
		var files = GetFiles("./*.sln");
		foreach(var file in files) 
		{
			DotNetTest(file.FullPath, testSettings);
		}
    });

Task("Report")
    .IsDependentOn("Test")
    .Does(() =>
{
    var reportSettings = new ReportGeneratorSettings
    {
        ArgumentCustomization = args => args
			.Append("-reportTypes:Html;TextSummary")
    };	
	
    var coverageDirectory = Directory("./TestResults");
    var files = GetFiles("./**/TestResults/*/coverage.cobertura.xml");
    ReportGenerator(files, coverageDirectory, reportSettings);
    
    // print summaries to console
    var summaries = GetFiles($"{coverageDirectory}/Summary.txt");
    foreach(var file in summaries) 
    {
        var summary = FileReadText(file);
        AnsiConsole.Markup($"[teal]{summary}[/]");
    }
});

Task("Format")
    .Does(() =>
{
    var projects = GetFiles("./*.csproj}").Select(p => p.FullPath);
    foreach(var project in projects)
    {
        DotNetCoreTool($"dotnet-format {project}");
    }
});

Task("Default").Does(() => {
    DotNetCoreTool("cake", new DotNetCoreToolSettings{
        ArgumentCustomization = args => args.Append("--showdescription")
    });
});

RunTarget(target);
#tool "nuget:?package=ReportGenerator&version=5.4.3"
#addin nuget:?package=Cake.Coverlet&version=4.0.1
using Spectre.Console

var target = Argument("target", "Default");
var configuration = Argument("Configuration", "Debug");
var solution = "./BotDeScans.sln";
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
        DotNetBuild(solution, new DotNetBuildSettings()
			{
				Configuration = configuration
			});
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {	
		CleanDirectory(testResultsDir);
	
		var coverletSettings = new CoverletSettings {
			CollectCoverage = true,
			CoverletOutputFormat = CoverletOutputFormat.cobertura,
			CoverletOutputDirectory = Directory(testResultsDir),
			CoverletOutputName = "coverage",
			ExcludeByAttribute = ["GeneratedCodeAttribute"],
			Exclude = ["**BotDeScans.App.Infra.Migrations**"]
		};
		
		DotNetTest(solution, new() { NoBuild = true }, coverletSettings);
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
    var files = GetFiles("./**/TestResults/coverage.cobertura.xml");
    ReportGenerator(files, coverageDirectory, reportSettings);
    
    // print summaries to console
    var summaries = GetFiles($"{coverageDirectory}/Summary.txt");
    foreach(var file in summaries) 
    {
        var summary = System.IO.File.ReadAllText(file.FullPath);
        AnsiConsole.Markup($"[teal]{summary}[/]");
    }
});

Task("Format")
    .Does(() =>
{
    var projects = GetFiles("./*.csproj}").Select(p => p.FullPath);
    foreach(var project in projects)
    {
        DotNetTool($"dotnet-format {project}");
    }
});

Task("Default").Does(() => {
    DotNetTool("cake", new DotNetToolSettings{
        ArgumentCustomization = args => args.Append("--showdescription")
    });
});

RunTarget(target);
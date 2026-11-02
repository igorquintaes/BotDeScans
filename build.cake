#tool "nuget:?package=ReportGenerator&version=5.4.3"
// O addin Cake.Coverlet foi removido aqui
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
        DotNetTest(solution, new DotNetTestSettings 
        { 
            Configuration = configuration,
            NoBuild = true,
            Verbosity = DotNetVerbosity.Normal,
            ArgumentCustomization = args => args
                .Append("--")
                .Append("--coverage")
                .Append("--coverage-output-format")
                .Append("cobertura") 
        });
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

        var coverageDirectory = Directory("./TestResults"); // Pasta final unificada
    
        // O curinga ** rastreia a solution inteira buscando os arquivos gerados pelo MTP
        var files = GetFiles("./**/*.cobertura.xml"); 
    
        ReportGenerator(files, coverageDirectory, reportSettings);

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
        var projects = GetFiles("./*.csproj").Select(p => p.FullPath); // Corrigido o erro de digitação original "csproj}"
        foreach(var project in projects)
        {
            DotNetTool($"dotnet-format {project}");
        }
    });

Task("Default")
    .Does(() => 
    {
        DotNetTool("cake", new DotNetToolSettings{
            ArgumentCustomization = args => args.Append("--showdescription")
        });
    });

RunTarget(target);
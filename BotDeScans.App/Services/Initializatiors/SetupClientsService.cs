using BotDeScans.App.Services.ExternalClients;
using BotDeScans.App.Services.GoogleDrive;
using BotDeScans.App.Services.Logging;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
namespace BotDeScans.App.Services.Initializatiors;

public class SetupClientsService(IServiceProvider serviceProvider, GoogleDriveSettingsService googleDriveSettingsService, LoggerService loggerService) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Initalizing external clients - Started");

        var clientsTypes = Assembly
            .GetAssembly(typeof(ExternalClientBase))!
            .GetTypes()
            .Where(type => type.BaseType is not null
                        && type.BaseType.IsGenericType 
                        && type.BaseType.GetGenericTypeDefinition() == typeof(ExternalClientBase<>));

        var finalResult = Result.Ok();
        foreach (var clientType in clientsTypes)
        {
            var client = (ExternalClientBase)serviceProvider.GetRequiredService(clientType);
            var result = await client.InitializeAsync(cancellationToken);
            finalResult = Result.Merge(finalResult, result);
        }

        if (finalResult.IsFailed)
            loggerService.LogErrors("Failed to setup external clients.", finalResult.Errors);

        Console.WriteLine("Initalizing external clients - Done");
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Setting up Google Drive Base Folder - Started");

        var result = await googleDriveSettingsService.SetUpBaseFolderAsync(stoppingToken);
        if (result.IsFailed)
            loggerService.LogErrors("Error while trying to setup Google Drive base folder.", result.Errors);

        Console.WriteLine("Setting up Google Drive Base Folder - Done");
    }
}

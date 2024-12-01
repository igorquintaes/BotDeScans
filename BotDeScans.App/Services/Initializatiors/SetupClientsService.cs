using BotDeScans.App.Features.GoogleDrive.InternalServices;
using BotDeScans.App.Services.ExternalClients;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace BotDeScans.App.Services.Initializatiors;

public class SetupClientsService(IServiceProvider serviceProvider, GoogleDriveSettingsService googleDriveSettingsService)
{
    public async Task<Result> SetupAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Initalizing External Clients...");

        var clientsTypes = Assembly
            .GetAssembly(typeof(ExternalClientBase))!
            .GetTypes()
            .Where(type => type.BaseType is not null
                        && type.BaseType.IsGenericType
                        && type.BaseType.GetGenericTypeDefinition() == typeof(ExternalClientBase<>));

        foreach (var clientType in clientsTypes)
        {
            var client = (ExternalClientBase)serviceProvider.GetRequiredService(clientType);
            var result = await client.InitializeAsync(cancellationToken);
            if (result.IsFailed)
                return result;
        }

        Console.WriteLine("Setting up Google Drive Base Folder...");
        return await googleDriveSettingsService.SetUpBaseFolderAsync(cancellationToken);
    }
}

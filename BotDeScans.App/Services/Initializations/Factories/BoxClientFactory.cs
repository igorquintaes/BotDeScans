using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories.Base;
using Box.Sdk.Gen;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Initializations.Factories;

public class BoxClientFactory(IConfiguration configuration) : ClientFactory<IBoxClient>
{
    public override bool Enabled => configuration
        .GetValues<StepName>("Settings:Publish:Steps")
        .Any(x => x is StepName.UploadPdfBox or StepName.UploadZipBox);

    [ExcludeFromCodeCoverage(Justification = "BoxJWTAuth is not mockable  - all code relies this class.")]
    public override Task<Result<IBoxClient>> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var clientId = configuration.GetRequiredValue<string>("Box:ClientId");
        var clientSecret = configuration.GetRequiredValue<string>("Box:ClientSecret");

        var config = new CcgConfig(clientId, clientSecret);
        var auth = new BoxCcgAuth(config);
        var client = new BoxClient(auth);
        var result = Result.Ok<IBoxClient>(client);

        return Task.FromResult(result);
    }

    public override async Task<Result> HealthCheckAsync(IBoxClient client, CancellationToken cancellationToken)
    {
        var folder = await client.Folders.GetFolderByIdAsync(BoxService.ROOT_ID, cancellationToken: cancellationToken);
        return Result.OkIf(folder is not null && 
                           folder.ItemCollection is not null && 
                           folder.ItemCollection.TotalCount == 1, 
               error: "Unknown error while trying to retrieve information from account.");
    }
}
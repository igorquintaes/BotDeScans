using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services.Initializations.Factories.Base;
using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.Factories;

public class BoxClientFactory(IConfiguration configuration) : ClientFactory<IBoxClient>
{
    public const string CREDENTIALS_FILE_NAME = "box.json";

    public override bool Enabled => configuration
        .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepName), value))
        .Any(x => x is StepName.UploadPdfBox or StepName.UploadZipBox);

    public override async Task<Result<IBoxClient>> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        await using var credentialStream = GetConfigFileAsStream(CREDENTIALS_FILE_NAME).Value;
        var config = BoxConfig.CreateFromJsonFile(credentialStream);
        var boxJwt = new BoxJWTAuth(config);
        var adminToken = await boxJwt.AdminTokenAsync();
        return boxJwt.AdminClient(adminToken);
    }

    public override async Task<Result> HealthCheckAsync(IBoxClient client, CancellationToken cancellationToken)
    {
        var accInfo = await client.FoldersManager.GetFolderItemsAsync("0", 1);
        return Result.OkIf(accInfo is not null, "Unknown error while trying to retrieve information from account.");
    }
}
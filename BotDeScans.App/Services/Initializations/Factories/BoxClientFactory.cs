using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories.Base;
using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace BotDeScans.App.Services.Initializations.Factories;

public class BoxClientFactory(IConfiguration configuration) : ClientFactory<IBoxClient>
{
    public const string CREDENTIALS_FILE_NAME = "box.json";

    public override bool Enabled => configuration
        .GetValues<StepName>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepName), value))
        .Any(x => x is StepName.UploadPdfBox or StepName.UploadZipBox);

    [ExcludeFromCodeCoverage(Justification = "BoxJWTAuth is not mockable  - all code relies this class.")]
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
        var accInfo = await client.FoldersManager.GetFolderItemsAsync(BoxService.ROOT_ID, limit: 1);
        return Result.OkIf(accInfo is not null && accInfo.TotalCount == 1, "Unknown error while trying to retrieve information from account.");
    }
}
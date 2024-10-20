using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using Box.V2;
using Box.V2.Config;
using Box.V2.JWTAuth;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services.ExternalClients;

public class BoxClient(IConfiguration configuration) : ExternalClientBase<IBoxClient>
{
    protected override bool Enabled => configuration
        .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value))
        .Any(x => x == StepEnum.UploadPdfBox || x == StepEnum.UploadZipBox);

    public override async Task<Result> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (Client is not null || !Enabled)
            return Result.Ok();

        try
        {
            var credentialResult = GetCredentialsAsStream("box.json");
            if (credentialResult.IsFailed)
                return credentialResult.ToResult();

            using var stream = credentialResult.Value;
            var config = BoxConfig.CreateFromJsonFile(stream);
            var boxJwt = new BoxJWTAuth(config);
            var adminToken = await boxJwt.AdminTokenAsync();
            Client = boxJwt.AdminClient(adminToken);

            var accInfo = await Client.FoldersManager.GetFolderItemsAsync("0", 1);
            return Result.OkIf(accInfo is not null, "Unknown error while trying to retrieve information from account.");
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Unable to create Box client.")
                          .CausedBy(ex));
        }
    }
}

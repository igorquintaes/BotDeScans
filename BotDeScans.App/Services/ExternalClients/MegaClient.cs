using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using CG.Web.MegaApiClient;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Services.ExternalClients;

public class MegaClient(IConfiguration configuration) : ExternalClientBase<IMegaApiClient>
{
    protected override bool Enabled => configuration
        .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value))
        .Any(x => x == StepEnum.UploadPdfMega || x == StepEnum.UploadZipMega);

    public override async Task<Result> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (Client is not null || !Enabled)
            return Result.Ok();

        try
        {
            var user = configuration.GetValue<string?>("Mega:User");
            if (string.IsNullOrWhiteSpace(user)) Result.Fail("'Mega:User' config.json value not found.");

            var pass = configuration.GetValue<string?>("Mega:Pass");
            if (string.IsNullOrWhiteSpace(pass)) Result.Fail("'Mega:Pass' config.json value not found.");

            Client = new MegaApiClient();
            await Client.LoginAsync(user, pass);
            if (!Client.IsLoggedIn)
                return Result.Fail("Unable to login on Mega. Check your user and pass, or if your account is blocked.");

            var accInfo = await Client.GetAccountInformationAsync();
            return Result.OkIf(accInfo is not null, "Error while trying to retrieve information from account.");
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Error while creating Mega client.")
                         .CausedBy(ex));
        }
    }
}

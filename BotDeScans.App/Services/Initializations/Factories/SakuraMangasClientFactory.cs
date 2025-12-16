using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.Initializations.Factories.Base;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace BotDeScans.App.Services.Initializations.Factories;

public class SakuraMangasClientFactory(IConfiguration configuration) : ClientFactory<SakuraMangasService>
{
    public override bool Enabled => configuration
        .GetValues<StepName>("Settings:Publish:Steps")
        .Any(x => x is StepName.UploadSakuraMangas);

    public override Task<Result<SakuraMangasService>> CreateAsync(CancellationToken cancellationToken)
    {
        const string USER_AGENT_NAME = "BotDeScans";
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        var userAgent = $"{USER_AGENT_NAME}/{appVersion}";

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var username = configuration.GetRequiredValue<string>("SakuraMangas:User");
        var password = configuration.GetRequiredValue<string>("SakuraMangas:Pass");

        var service = new SakuraMangasService(httpClient, username, password);
        return Task.FromResult(Result.Ok(service));
    }

    public override async Task<Result> HealthCheckAsync(SakuraMangasService client, CancellationToken cancellationToken) =>
        await client.PingCredentialsAsync(cancellationToken);
}

using BotDeScans.App.Extensions;
using BotDeScans.App.Models.Entities.Enums;
using BotDeScans.App.Services.CapivaraTradutor;
using BotDeScans.App.Services.CapivaraTradutor.InternalServices;
using BotDeScans.App.Services.Initializations.Factories.Base;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Reflection;

namespace BotDeScans.App.Services.Initializations.Factories;

public class CapivaraTradutorClientFactory(IConfiguration configuration, CapivaraTradutorUploadService capivaraTradutorUploadService) : ClientFactory<CapivaraTradutorService>
{
    public override bool Enabled => configuration
        .GetValues<StepName>("Settings:Publish:Steps")
        .Any(x => x is StepName.UploadTradutorCapivara);

    public override Task<Result<CapivaraTradutorService>> CreateAsync(CancellationToken cancellationToken)
    {
        const string USER_AGENT_NAME = "BotDeScans";
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        var userAgent = $"{USER_AGENT_NAME}/{appVersion}";
        var groupId = configuration.GetRequiredValue<string>("Capivara:GroupId");
        var apiKey = configuration.GetRequiredValue<string>("Capivara:ApiKey");

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("GroupId", groupId);
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var service = new CapivaraTradutorService(httpClient, capivaraTradutorUploadService);
        return Task.FromResult(Result.Ok(service));
    }

    public override async Task<Result> HealthCheckAsync(CapivaraTradutorService client, CancellationToken cancellationToken) =>
        await client.PingCredentialsAsync(cancellationToken);
}

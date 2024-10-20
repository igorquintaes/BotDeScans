using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace BotDeScans.App.Services.ExternalClients;

public class TsukiClient : ExternalClientBase<HttpClient>
{
    public static readonly JsonSerializerOptions Configuration = new() { PropertyNameCaseInsensitive = true };
    private readonly IConfiguration configuration;

    public TsukiClient(HttpClient httpClient, IConfiguration configuration)
    {
        Client = httpClient;
        this.configuration = configuration;
    }

    protected override bool Enabled => configuration
        .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value))
        .Any(x => x == StepEnum.UploadTsuki);

    public override async Task<Result> InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!Enabled) return Result.Ok();
        if (Enabled) return Result.Fail("Tsuki atualmente não suportada por limitações da Cloud Flare.");

        const string LOGIN_URL = "https://tsuki-mangas.com/api/v3/login";
        var username = configuration.GetRequiredValue<string>("Tsuki:User");
        var password = configuration.GetRequiredValue<string>("Tsuki:Pass");
        var request = new { Username = username, Password = password };

        var loginHttpResult = await Client.PostAsJsonAsync(LOGIN_URL, request, cancellationToken);
        var loginResultData = await loginHttpResult.TryGetContent<LoginResponse>(Configuration, cancellationToken);
        if (loginResultData.IsFailed)
            return loginResultData.ToResult();

        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResultData.Value.Token}");
        return Result.Ok();
    }

    public static HttpClient SetupClient(HttpClient httpClient)
    {
        const string USER_AGENT_NAME = "BotDeScans";
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        var userAgent = $"{USER_AGENT_NAME}/{appVersion}";
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        return httpClient;
    }

    public record LoginResponse(string Token);
}

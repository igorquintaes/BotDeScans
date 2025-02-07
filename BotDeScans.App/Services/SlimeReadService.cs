using BotDeScans.App.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace BotDeScans.App.Services;

public class SlimeReadService(
    HttpClient httpClient,
    IConfiguration configuration)
{
    public const string BASE_URL = "slimeread.com";

    private string? accessToken = null;

    public async Task<Result> LoginAsync(CancellationToken cancellationToken)
    {
        var username = configuration.GetRequiredValue<string>("SlimeRead:Username");
        var password = configuration.GetRequiredValue<string>("SlimeRead:Password");

        var requestData = new { name = username, password };
        var requestDataAsJson = JsonSerializer.Serialize(requestData);
        using var request = new StringContent(requestDataAsJson, Encoding.UTF8, "application/json");

        var loginResult = await httpClient.PostAsync(
            "https://tipaeupapai.slimeread.com:8443/login",
            request,
            cancellationToken);

        if (loginResult.IsSuccessStatusCode is false)
            return Result.Fail($"Erro ao logar na SlimeRead. StatusCode {loginResult.StatusCode}.");

        var loginContentAsString = await loginResult.Content.ReadAsStringAsync(cancellationToken);
        var loginContent = JsonSerializer.Deserialize<LoginResponse>(loginContentAsString);

        if (loginContent is null || string.IsNullOrWhiteSpace(loginContent.Token))
            return Result.Fail(loginContentAsString);

        accessToken = loginContent.Token;
        return Result.Ok();
    }
}

public record LoginResponse(string Token);

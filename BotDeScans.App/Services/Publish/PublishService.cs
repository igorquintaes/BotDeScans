using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Publish.Steps;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Publish;

public class PublishService(
    IConfiguration configuration,
    IDiscordRestGuildAPI discordRestGuildAPI,
    PublishState publishState,
    IEnumerable<IStep> steps)
{
    private readonly IEnumerable<IStep> steps = steps;
    private readonly PublishState publishState = publishState;


    public async Task<Result<string>> GetPublishPingAsync(string title, CancellationToken cancellationToken)
    {
        const string pingTypeKey = "Settings:Publish:PingType";
        var pingType = configuration.GetRequiredValue<PingType>(pingTypeKey);

        return pingType switch
        {
            PingType.Everyone => "@everyone",
            PingType.Global => await GetGlobalAndTitleRoles(title, cancellationToken),
            PingType.Role => await GetTitleRole(title, cancellationToken),
            PingType.None => string.Empty,
            _ => Result.Fail($"invalid value in '{pingTypeKey}'."),
        };
    }

    private async Task<Result<string>> GetTitleRole(string title, CancellationToken cancellationToken) 
        => TryGetRoleName(title, out var roleName) is false
            ? (Result<string>)Result.Fail($"Erro ao encontrar um cargo para o mangá '{title}', no arquivo roles.txt")
            : await GetRoleFromDiscord(roleName, cancellationToken);

    private async Task<Result<string>> GetGlobalAndTitleRoles(string title, CancellationToken cancellationToken)
    {
        var titleRolePing = await GetTitleRole(title, cancellationToken);
        if (titleRolePing.IsFailed)
            return titleRolePing;

        const string globalRoleKey = "Settings:Publish:GlobalRole";
        var globalRoleName = configuration.GetRequiredValue<string>(globalRoleKey);
        var globalRolePing = await GetRoleFromDiscord(globalRoleName, cancellationToken);
        if (globalRolePing.IsFailed)
            return globalRolePing;

        return $"{titleRolePing.Value}, {globalRolePing.Value}";
    }

    private async Task<Result<string>> GetRoleFromDiscord(string roleName, CancellationToken cancellationToken)
    {
        var serverId = configuration.GetRequiredValue<ulong>("Discord:ServerId");
        var guildRolesResult = await discordRestGuildAPI.GetGuildRolesAsync(new Snowflake(serverId), cancellationToken);

        if (!guildRolesResult.IsDefined(out var guildRoles))
            return Result.Fail(guildRolesResult.Error!.Message);

        var guildRole = guildRoles.FirstOrDefault(guildRole => roleName.Equals(guildRole.Name, StringComparison.Ordinal));
        return guildRole is not null
            ? Result.Ok($"<@&{guildRole.ID.Value}>")
            : Result.Fail("Cargo não encontrado no servidor.");
    }

    // isso vai morrer, pode continuar feio
    private static bool TryGetRoleName(string title, out string roleName) =>
        File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "roles.txt"))
            .Select(x => x.Split("$"))
            .ToDictionary(x => x[0].Trim().ToLowerInvariant(), x => x[1].Trim())
            .TryGetValue(title.ToLowerInvariant(), out roleName!);

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken)
        => RunAsync(
            operation: "validating",
            func: async (step, ct) => await step.ValidateBeforeFilesManagementAsync(ct),
            stepTypes: Enum.GetValues<StepType>(),
            breakOnError: false,
            changeStateOnSuccess: false,
            cancellationToken: cancellationToken);

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken cancellationToken)
        => RunAsync(
            operation: "validating",
            func: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
            stepTypes: Enum.GetValues<StepType>(),
            breakOnError: false,
            changeStateOnSuccess: false,
            cancellationToken: cancellationToken);

    public Task<Result> RunAsync(StepType stepType, CancellationToken cancellationToken)
        => RunAsync(
            operation: "executing",
            func: async (step, ct) => await step.ExecuteAsync(ct),
            stepTypes: [stepType],
            breakOnError: true,
            changeStateOnSuccess: true,
            cancellationToken: cancellationToken);

    private async Task<Result> RunAsync(
        string operation,
        Func<IStep, CancellationToken, Task<Result>> func,
        StepType[] stepTypes,
        bool breakOnError,
        bool changeStateOnSuccess,
        CancellationToken cancellationToken)
    {
        var result = new Result();
        foreach (var step in steps
            .Where(x => publishState.Steps[x.StepName] != StepStatus.Skip)
            .Where(x => stepTypes.Contains(x.StepType))
            .OrderBy(x => x.StepName))
        {
            if (changeStateOnSuccess)
                publishState.Steps[step.StepName] = StepStatus.Executing;

            try
            {
                var executionResult = await func(step, cancellationToken);
                result.WithReasons(executionResult.Reasons);

                if (changeStateOnSuccess)
                    publishState.Steps[step.StepName] = executionResult.IsSuccess
                        ? StepStatus.Success
                        : StepStatus.Error;
                else if (executionResult.IsFailed)
                    publishState.Steps[step.StepName] = StepStatus.Error;

            }
            catch (Exception ex)
            {
                var message = $"Unexpected error while {operation} {step.StepName}. More info inside exception logs.";
                result.WithError(new Error(message).CausedBy(ex));
                publishState.Steps[step.StepName] = StepStatus.Error;
            }

            // todo: no futuro podemos pensar em cenários de falha e que permitem o fluxo continuar... não agora.
            if (publishState.Steps[step.StepName] == StepStatus.Error && breakOnError)
                break;
        }

        return result;
    }

    // todo: método de revert no futuro (?)
    // talvez não seja necessário... estamos ampliando os casos validação
    // podemos tratar como cenários exceção e reversão manual em caso de erros.
}

public enum PingType
{
    Everyone,
    Global,
    Role,
    None
}
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Services.Discord;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish;

public class PublishService(
    IConfiguration configuration,
    RolesService rolesService,
    PublishState publishState,
    IEnumerable<IStep> steps)
{
    private readonly IEnumerable<IStep> steps = steps;
    private readonly PublishState publishState = publishState;

    public async Task<Result<string>> CreatePingMessageAsync(CancellationToken cancellationToken)
    {
        const string pingTypeKey = "Settings:Publish:PingType";
        var pingType = configuration.GetRequiredValue<PingType>(pingTypeKey);

        switch (pingType)
        {
            case PingType.Everyone:
                return "@everyone";
            case PingType.Global:
                if (publishState.Title.DiscordRoleId is null)
                    return Result.Fail("Não foi definida uma role para o Discord nesta obra. Defina, ou mude o tipo de publicação no arquivo de configuração do Bot de Scans.");

                const string globalRoleKey = "Settings:Publish:GlobalRole";
                var globalRoleName = configuration.GetRequiredValue<string>(globalRoleKey);
                var globalRoleAsPingResult = await GetRoleAsPingText(globalRoleName, cancellationToken);
                if (globalRoleAsPingResult.IsFailed)
                    return globalRoleAsPingResult;

                var titleRoleAsPingResult = await GetRoleAsPingText(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);
                if (titleRoleAsPingResult.IsFailed)
                    return titleRoleAsPingResult;

                return $"{globalRoleAsPingResult.Value}, {titleRoleAsPingResult.Value}";
            case PingType.Role:
                if (publishState.Title.DiscordRoleId is null)
                    return Result.Fail("Não foi definida uma role para o Discord nesta obra. Defina, ou mude o tipo de publicação no arquivo de configuração do Bot de Scans.");

                return await GetRoleAsPingText(publishState.Title.DiscordRoleId.ToString()!, cancellationToken);
            case PingType.None:
                return string.Empty;
            default:
                return Result.Fail($"invalid value in '{pingTypeKey}'.");
        };
    }

    // todo: mover para uma classe que faça mais sentido (talvez relacionada ao discord)
    private async Task<Result<string>> GetRoleAsPingText(string roleName, CancellationToken cancellationToken)
    {
        var role = await rolesService.GetRoleFromGuildAsync(roleName, cancellationToken);
        if (role.IsFailed)
            return role.ToResult();

        return $"<@&{role.Value.ID.Value}>";
    }

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken)
        => RunStepsAsync(
            stepFunc: async (step, ct) => await step.ValidateBeforeFilesManagementAsync(ct),
            feedbackFunc: null,
            stepTypes: Enum.GetValues<StepType>(),
            breakOnError: false,
            changeStateOnSuccess: false,
            cancellationToken: cancellationToken);

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken cancellationToken)
        => RunStepsAsync(
            stepFunc: async (step, ct) => await step.ValidateAfterFilesManagementAsync(ct),
            feedbackFunc: null,
            stepTypes: Enum.GetValues<StepType>(),
            breakOnError: false,
            changeStateOnSuccess: false,
            cancellationToken: cancellationToken);

    public Task<Result> RunManagementStepsAsync(
        Func<Task<Result>>? feedbackFunc,
        CancellationToken cancellationToken)
        => RunStepsAsync(
            stepFunc: async (step, ct) => await step.ExecuteAsync(ct),
            feedbackFunc: feedbackFunc,
            stepTypes: [StepType.Management],
            breakOnError: true,
            changeStateOnSuccess: true,
            cancellationToken: cancellationToken);

    public Task<Result> RunPublishStepsAsync(
        Func<Task<Result>>? feedbackFunc,
        CancellationToken cancellationToken)
        => RunStepsAsync(
            stepFunc: async (step, ct) => await step.ExecuteAsync(ct),
            feedbackFunc: feedbackFunc,
            stepTypes: [StepType.Publish],
            breakOnError: true,
            changeStateOnSuccess: true,
            cancellationToken: cancellationToken);

    private async Task<Result> RunStepsAsync(
        Func<IStep, CancellationToken, Task<Result>> stepFunc,
        Func<Task<Result>>? feedbackFunc,
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
                var executionResult = await stepFunc(step, cancellationToken);
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
                var message = $"Unexpected error in {step.StepName}. " +
                              $"Exception message: {ex.Message}. " +
                               "More info inside exception logs.";

                result.WithError(new Error(message).CausedBy(ex));
                publishState.Steps[step.StepName] = StepStatus.Error;
            }

            if (feedbackFunc is not null)
                await feedbackFunc();

            // todo: no futuro podemos pensar em cenários de falha e que permitem o fluxo continuar... não agora.
            if (publishState.Steps[step.StepName] == StepStatus.Error && breakOnError)
                break;
        }

        return result;
    }

    // todo: método de revert no futuro (?)
    // talvez não seja necessário... estamos ampliando os casos validação e casos de rewrite
    // podemos tratar como cenários exceção e reversão manual em caso de erros.
}

public enum PingType
{
    Everyone,
    Global,
    Role,
    None
}
using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Discord;
using BotDeScans.App.Features.Publish.Pings;
using BotDeScans.App.Services.Discord;
using BotDeScans.App.Services.Wrappers;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Remora.Rest.Core;
namespace BotDeScans.App.Services.Initializations;

public class SetupDiscordService(
    SlashServiceWrapper slashService, 
    IConfiguration configuration)
{
    public virtual async Task<Result> SetupAsync(CancellationToken cancellationToken)
    {
        // todo: podemos pensar em habilitar comando somente do que está configurado e validado
        // ex: mega está no publish? habilitamos! Não está? não habilitamos!
        Console.WriteLine("Updating Discord Slash Commands...");

        var serverIdResult = configuration.GetRequiredValue<ulong>("Discord:ServerId");
        var updateSlashResult = await slashService.UpdateSlashCommandsAsync(new Snowflake(serverIdResult), ct: cancellationToken);
        return updateSlashResult.IsSuccess is false
            ? Result.Fail("Failed to update Discord slash commands.")
            : Result.Ok();
    }
}

public class SetupDiscordServiceValidator : AbstractValidator<SetupDiscordService>
{
    public SetupDiscordServiceValidator(
        RolesService rolesService,
        IConfiguration configuration)
    {
        var discordTokenResult = configuration.GetRequiredValueAsResult<string>("Discord:Token");
        var releaseChannelResult = configuration.GetRequiredValueAsResult<ulong>("Discord:ReleaseChannel");
        var serverIdResult = configuration.GetRequiredValueAsResult<ulong>("Discord:ServerId");
        var pingTypeResult = configuration.GetRequiredValueAsResult<PingType>(Ping.PING_TYPE_KEY);

        RuleFor(service => service)
            .Must(_ => discordTokenResult.IsSuccess)
            .WithMessage(discordTokenResult.ToValidationErrorMessage());

        RuleFor(service => service)
            .Must(_ => releaseChannelResult.IsSuccess)
            .WithMessage(releaseChannelResult.ToValidationErrorMessage());

        RuleFor(service => service)
            .Must(_ => serverIdResult.IsSuccess)
            .WithMessage(serverIdResult.ToValidationErrorMessage());

        RuleFor(service => service)
            .Must(_ => pingTypeResult.IsSuccess)
            .WithMessage(pingTypeResult.ToValidationErrorMessage());

        When(_ => pingTypeResult.ValueOrDefault is PingType.Global, () =>
        {
            var globalPingResult = configuration.GetRequiredValueAsResult<string>(GlobalPing.GLOBAL_ROLE_KEY);

            RuleFor(service => service)
                .Must(_ => globalPingResult.IsSuccess)
                .WithMessage(globalPingResult.ToValidationErrorMessage());

            RuleFor(service => service)
                .MustAsync(async (_, _, context, ct) => await RoleMustExists(globalPingResult.Value, rolesService, context, ct))
                .When(_ => globalPingResult.IsSuccess);
        });
    }

    private static async Task<bool> RoleMustExists(
        string roleValue,
        RolesService rolesService,
        ValidationContext<SetupDiscordService> context,
        CancellationToken cancellationToken)
    {
        var rolesResult = await rolesService.GetRoleFromGuildAsync(roleValue, cancellationToken);
        if (rolesResult.IsSuccess)
            return true;

        context.AddFailure(rolesResult.ToValidationErrorMessage());
        return true;
    }
}
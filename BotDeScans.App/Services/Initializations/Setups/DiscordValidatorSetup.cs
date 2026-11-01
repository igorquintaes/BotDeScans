using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Services.Discord;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Services.Initializations.Setups;

public class DiscordValidatorSetup(IValidator<DiscordValidator> validator) : ISetup
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(new DiscordValidator(), cancellationToken);
        var result = validationResult.ToResult();

        if (result.IsFailed)
            return result;

        return result.WithSuccess("Discord validation data completed");
    }
}

public class DiscordValidatorSetupValidator : AbstractValidator<DiscordValidator>
{
    public DiscordValidatorSetupValidator(
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
                .CustomAsync(async (_, context, ct) => await RoleMustExists(globalPingResult.Value, rolesService, context, ct))
                .When(_ => globalPingResult.IsSuccess);
        });
    }

    private static async Task<bool> RoleMustExists(
        string roleValue,
        RolesService rolesService,
        ValidationContext<DiscordValidator> context,
        CancellationToken cancellationToken)
    {
        var rolesResult = await rolesService.GetRoleAsync(roleValue, cancellationToken);
        if (rolesResult.IsSuccess)
            return true;

        context.AddFailure(rolesResult.ToValidationErrorMessage());
        return false;
    }
}

public record DiscordValidator();
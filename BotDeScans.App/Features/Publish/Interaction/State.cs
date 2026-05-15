using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using BotDeScans.App.Features.Publish.Interaction.Models;

namespace BotDeScans.App.Features.Publish.Interaction;

public record State
{
    public EnabledSteps Steps { get; init; } = null!;
    public Title Title { get; init; } = null!;
    public Info ChapterInfo { get; init; } = null!;

    public string OriginContentFolder { get; init; } = null!;
    public string CoverFilePath { get; init; } = null!;
    public string? ZipFilePath { get; init; }
    public string? PdfFilePath { get; init; }
    public string? BloggerImageAsBase64 { get; init; }
    public string? BoxPdfReaderKey { get; init; }
    public string? Pings { get; init; }
    public TrackingMessage? TrackingMessage { get; init; }

    [ReleaseLink("Mega [Zip]")]
    public string? MegaZipLink { get; init; }

    [ReleaseLink("Mega [Pdf]")]
    public string? MegaPdfLink { get; init; }

    [ReleaseLink("Drive [Zip]")]
    public string? DriveZipLink { get; init; }

    [ReleaseLink("Drive [Pdf]")]
    public string? DrivePdfLink { get; init; }

    [ReleaseLink("Box [Zip]")]
    public string? BoxZipLink { get; init; }

    [ReleaseLink("Box [Pdf]")]
    public string? BoxPdfLink { get; init; }

    [ReleaseLink("MangaDex")]
    public string? MangaDexLink { get; init; }

    [ReleaseLink("Sakura Mangás")]
    public string? SakuraMangasLink { get; init; }

    [ReleaseLink("Blogger")]
    public string? BloggerLink { get; init; }
}

public class StateValidator : AbstractValidator<State>
{
    public StateValidator(
        RolesService rolesService,
        IConfiguration configuration,
        IValidator<Info> infoValidator,
        IValidator<Title> titleValidator)
    {
        RuleFor(model => model.ChapterInfo)
            .SetValidator(infoValidator);

        RuleFor(model => model.Title)
            .SetValidator(titleValidator);

        RuleFor(model => model.Title)
            .Must(prop => prop.References.Any(reference => reference.Key == ExternalReference.MangaDex))
            .When(prop => prop.Steps.Any(step => step.Key is UploadMangaDexStep))
            .WithMessage("Não foi definida uma referência para a publicação da obra na MangaDex.");

        RuleFor(model => model.Title)
            .Must(prop => prop.References.Any(reference => reference.Key == ExternalReference.SakuraMangas))
            .When(prop => prop.Steps.Any(step => step.Key is UploadSakuraMangasStep))
            .WithMessage("Não foi definida uma referência para a publicação da obra na Sakura Mangás.");

        var globalPingValue = configuration.GetValue<string?>(GlobalPing.GLOBAL_ROLE_KEY, null);
        var pingTypeAsString = configuration.GetValue<string?>(Ping.PING_TYPE_KEY, null);
        var isPingTypeValid = Enum.TryParse<PingType>(pingTypeAsString, out var pingType);

        RuleFor(model => model)
            .Cascade(CascadeMode.Stop)
            .Must(_ => string.IsNullOrWhiteSpace(globalPingValue) is false)
            .When(_ => pingType == PingType.Global)
            .WithMessage("É necessário definir um valor para ping global no arquivo de configuração do Bot de Scans.")
            .DependentRules(() =>
            {
                RuleFor(model => model)
                    .MustAsync(async (_, prop, context, cancellationToken) => await RoleMustExists(globalPingValue!, rolesService, context, cancellationToken))
                    .When(prop => pingType is PingType.Global);
            });
    }

    private static async Task<bool> RoleMustExists(
        string role,
        RolesService rolesService,
        ValidationContext<State> context,
        CancellationToken cancellationToken)
    {
        var rolesResult = await rolesService.GetRoleAsync(role, cancellationToken);
        if (rolesResult.IsSuccess)
            return true;

        context.AddFailure(string.Join("; ", rolesResult.Errors.Select(error => error.Message)));
        return false;
    }
}
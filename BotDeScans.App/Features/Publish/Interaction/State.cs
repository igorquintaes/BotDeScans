using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Pings;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Models.Entities;
using BotDeScans.App.Services.Discord;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction;

public record State
{
    public EnabledSteps Steps { get; init; } = null!;
    public Title Title { get; init; } = null!;
    public Info ChapterInfo { get; init; } = null!;
    public Links ReleaseLinks { get; init; } = new();
    public InternalData InternalData { get; init; } = new();

    public string OriginContentFolder => InternalData.OriginContentFolder;
    public string CoverFilePath => InternalData.CoverFilePath;
    public string? ZipFilePath => InternalData.ZipFilePath;
    public string? PdfFilePath => InternalData.PdfFilePath;
    public string? BloggerImageAsBase64 => InternalData.BloggerImageAsBase64;
    public string? BoxPdfReaderKey => InternalData.BoxPdfReaderKey;
    public string? Pings => InternalData.Pings;

    public string? MegaZipLink => ReleaseLinks.MegaZip;
    public string? MegaPdfLink => ReleaseLinks.MegaPdf;
    public string? DriveZipLink => ReleaseLinks.DriveZip;
    public string? DrivePdfLink => ReleaseLinks.DrivePdf;
    public string? BoxZipLink => ReleaseLinks.BoxZip;
    public string? BoxPdfLink => ReleaseLinks.BoxPdf;
    public string? MangaDexLink => ReleaseLinks.MangaDex;
    public string? SakuraMangasLink => ReleaseLinks.SakuraMangas;
    public string? BloggerLink => ReleaseLinks.Blogger;

    public State WithOriginContentFolder(string originContentFolder) =>
        this with { InternalData = InternalData with { OriginContentFolder = originContentFolder } };

    public State WithCoverFilePath(string coverFilePath) =>
        this with { InternalData = InternalData with { CoverFilePath = coverFilePath } };

    public State WithZipPath(string zipFilePath) =>
        this with { InternalData = InternalData with { ZipFilePath = zipFilePath } };

    public State WithPdfPath(string pdfFilePath) =>
        this with { InternalData = InternalData with { PdfFilePath = pdfFilePath } };

    public State WithBloggerImageAsBase64(string bloggerImageAsBase64) =>
        this with { InternalData = InternalData with { BloggerImageAsBase64 = bloggerImageAsBase64 } };

    public State WithBoxPdfReaderKey(string boxPdfReaderKey) =>
        this with { InternalData = InternalData with { BoxPdfReaderKey = boxPdfReaderKey } };

    public State WithPings(string pings) =>
        this with { InternalData = InternalData with { Pings = pings } };

    public State WithMegaZipLink(string link) => this with { ReleaseLinks = ReleaseLinks with { MegaZip = link } };
    public State WithMegaPdfLink(string link) => this with { ReleaseLinks = ReleaseLinks with { MegaPdf = link } };
    public State WithDriveZipLink(string link) => this with { ReleaseLinks = ReleaseLinks with { DriveZip = link } };
    public State WithDrivePdfLink(string link) => this with { ReleaseLinks = ReleaseLinks with { DrivePdf = link } };
    public State WithBoxZipLink(string link) => this with { ReleaseLinks = ReleaseLinks with { BoxZip = link } };
    public State WithBoxPdfLink(string link) => this with { ReleaseLinks = ReleaseLinks with { BoxPdf = link } };
    public State WithMangaDexLink(string link) => this with { ReleaseLinks = ReleaseLinks with { MangaDex = link } };
    public State WithSakuraMangasLink(string link) => this with { ReleaseLinks = ReleaseLinks with { SakuraMangas = link } };
    public State WithBloggerLink(string link) => this with { ReleaseLinks = ReleaseLinks with { Blogger = link } };
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
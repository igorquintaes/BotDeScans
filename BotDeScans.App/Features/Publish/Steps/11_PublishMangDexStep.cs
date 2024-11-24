using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class PublishMangadexStep(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadMangadex;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
    {
        if (TryGetMangaId(state.Info.DisplayTitle, out var _) is false)
            return Task.FromResult(Result.Fail("Obra não encontrada em 'mangadex-ids.txt'."));

        var username = configuration.GetValue<string?>("Mangadex:Username", null);
        var password = configuration.GetValue<string?>("Mangadex:Password", null);
        var clientId = configuration.GetValue<string?>("Mangadex:ClientId", null);
        var clientSecret = configuration.GetValue<string?>("Mangadex:ClientSecret", null);

        if (username is null || 
            password is null ||
            clientId is null ||
            clientSecret is null)
            return Task.FromResult(Result.Fail("As credenciais da MangaDex não estão preenchidas (parcialmente ou totalmente)."));

        return Task.FromResult(Result.Ok());
    }

    // API validations
    // (OK) 1 active upload session per account -> You need to either commit or abandon your current upload session before starting a new one 
    // (OK) Max 10 files per one PUT request
    // Max 20MB per file
    // Max 500 files per upload session
    // Max 150MB per upload session
    // (OK) File format must be JPEG, PNG, or GIF
    // File resolution must be below 10'000 pixels in both width and height 
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var mangaDexService = serviceProvider.GetRequiredService<MangaDexService>();

        var loginResult = await mangaDexService.LoginAsync();
        if (loginResult.IsFailed)
            return loginResult;

        var clearResult = await mangaDexService.ClearPendingUploadsAsync();
        if (clearResult.IsFailed)
            return clearResult;

        var uploadResult = await mangaDexService.UploadChapterAsync(
            state.Info.DisplayTitle,
            state.Info.ChapterName,
            state.Info.ChapterNumber,
            state.Info.ChapterVolume,
            state.InternalData.OriginContentFolder);

        if (uploadResult.IsFailed)
            return uploadResult.ToResult();

        state.Links.MangaDexLink = $"https://mangadex.org/chapter/{uploadResult.Value}/1";
        return Result.Ok();
    }

    // isso vai morrer, pode continuar feio
    private static bool TryGetMangaId(string mangaName, out string mangaId) =>
        File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "mangadex-ids.txt"))
            .Select(x => x.Split("$"))
            .ToDictionary(x => x[0].Trim().ToLowerInvariant(), x => x[1].Trim())
            .TryGetValue(mangaName.ToLowerInvariant(), out mangaId!);
}
﻿using BotDeScans.App.Enums;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Services.Publish.Steps;

public class PublishMangadexStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.UploadMangadex;
    public StepType StepType => StepType.Publish;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _) 
        => TryGetMangaId(state.Info.DisplayTitle, out var _) is false
        ? Task.FromResult(Result.Fail("Obra não encontrada em 'mangadex-ids.txt'."))
        : Task.FromResult(Result.Ok());

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

        var loginResult = await mangaDexService.LoginAsync(cancellationToken);
        if (loginResult.IsFailed)
            return loginResult;

        var clearResult = await mangaDexService.ClearPendingUploadsAsync(cancellationToken);
        if (clearResult.IsFailed)
            return clearResult;

        var uploadResult = await mangaDexService.UploadChapterAsync(
            state.Info.DisplayTitle,
            state.Info.ChapterName,
            state.Info.ChapterNumber,
            state.Info.ChapterVolume,
            state.InternalData.OriginContentFolder,
            cancellationToken);

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
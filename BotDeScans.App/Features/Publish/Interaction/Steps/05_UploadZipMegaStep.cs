﻿using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class UploadZipMegaStep(
    MegaService megaService,
    MegaSettingsService megaSettingsService,
    State state) : IPublishStep
{
    public StepType Type => StepType.Upload;
    public StepName Name => StepName.UploadZipMega;
    public StepName? Dependency => StepName.ZipFiles;

    public Task<Result> ValidateAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var root = await megaSettingsService.GetRootFolderAsync();
        var titleFolder = await megaService.GetOrCreateFolderAsync(state.Title.Name, root);
        if (titleFolder.IsFailed)
            return titleFolder.ToResult();

        var fileResult = await megaService.CreateFileAsync(
            filePath: state.InternalData.ZipFilePath!,
            parentNode: titleFolder.Value,
            cancellationToken);

        if (fileResult.IsFailed)
            return fileResult.ToResult();

        state.ReleaseLinks.MegaZip = fileResult.Value.AbsoluteUri;
        return Result.Ok();
    }
}

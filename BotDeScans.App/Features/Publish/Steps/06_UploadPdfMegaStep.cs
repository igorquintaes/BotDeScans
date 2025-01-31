﻿using BotDeScans.App.Features.Mega;
using BotDeScans.App.Features.Mega.InternalServices;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Features.Publish.Steps
{
    public class UploadPdfMegaStep(
        IServiceProvider serviceProvider,
        PublishState state) : IStep
    {
        public StepEnum StepName => StepEnum.UploadPdfMega;
        public StepType StepType => StepType.Publish;

        public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
            => Task.FromResult(Result.Ok());

        public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
            => Task.FromResult(Result.Ok());

        public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
        {
            var megaService = serviceProvider.GetRequiredService<MegaService>();
            var megaResourceService = serviceProvider.GetRequiredService<MegaResourcesService>();

            var root = await megaResourceService.GetRootNodeAsync();
            var titleFolder = await megaService.GetOrCreateFolderAsync(state.Title.Name, root);
            if (titleFolder.IsFailed)
                return titleFolder.ToResult();

            var fileResult = await megaService.CreateFileAsync(
                filePath: state.InternalData.PdfFilePath,
                parentNode: root,
                cancellationToken);

            if (fileResult.IsFailed)
                return fileResult.ToResult();

            state.ReleaseLinks.MegaPdf = fileResult.Value.AbsoluteUri;
            return Result.Ok();
        }
    }
}

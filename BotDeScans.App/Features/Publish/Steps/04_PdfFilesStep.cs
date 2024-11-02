using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class PdfFilesStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.PdfFiles;
    public StepType StepType => StepType.Manage;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var fileService = serviceProvider.GetRequiredService<FileService>();
        var fileReleaseService = serviceProvider.GetRequiredService<FileReleaseService>();

        state.InternalData.PdfFilePath = await fileService.CreatePdfFileAsync(
            fileName: $"{state.Info.ChapterNumber}.pdf",
            resourcesDirectory: state.InternalData.OriginContentFolder,
            destinationDirectory: fileReleaseService.CreateScopedDirectory());

        return Result.Ok();
    }
}

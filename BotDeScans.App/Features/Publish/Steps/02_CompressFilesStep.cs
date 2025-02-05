using BotDeScans.App.Services;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
namespace BotDeScans.App.Features.Publish.Steps;

public class CompressFilesStep(
    IServiceProvider serviceProvider,
    PublishState state) : IStep
{
    public StepEnum StepName => StepEnum.Compress;
    public StepType StepType => StepType.Management;

    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken _)
        => Task.FromResult(Result.Ok());

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var imageService = serviceProvider.GetRequiredService<ImageService>();
        var maxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0));
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(
            Directory.GetFiles(state.InternalData.OriginContentFolder),
            parallelOptions,
            async (filePath, ct) =>
            {
                var isGrayScale = imageService.IsGrayscale(filePath);
                await imageService.CompressImageAsync(filePath, isGrayScale, ct);
            });

        return Result.Ok();
    }
}

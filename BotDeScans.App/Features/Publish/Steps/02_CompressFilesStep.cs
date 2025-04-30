using BotDeScans.App.Features.Publish.State;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Services;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public class CompressFilesStep(
    ImageService imageService,
    PublishState state) : IManagementStep
{
    public StepType Type => StepType.Management;
    public StepName Name => StepName.Compress;

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var maxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.75 * 2.0));
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

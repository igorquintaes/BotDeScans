using BotDeScans.App.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsService(
    IConfiguration configuration,
    IStep[] steps)
{
    public IReadOnlyList<StepEnum> GetPublishSteps()
    {
        var configurationSteps = configuration
            .GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum
            .Parse(typeof(StepEnum), value));

        var publishSteps = new List<StepEnum>()
        {
            StepEnum.Download,
            StepEnum.Compress,
        };

        foreach (var configurationStep in configurationSteps)
        {
            var step = steps.Single(x => x.StepName == configurationStep);
            if (step.StepType == StepType.Management)
                continue;

            publishSteps.Add(step.StepName);

            if (configurationStep == StepEnum.UploadPdfBox |
                configurationStep == StepEnum.UploadPdfMega ||
                configurationStep == StepEnum.UploadPdfGoogleDrive)
                publishSteps.Add(StepEnum.PdfFiles);

            if (configurationStep == StepEnum.UploadZipBox ||
                configurationStep == StepEnum.UploadZipMega ||
                configurationStep == StepEnum.UploadZipGoogleDrive ||
                configurationStep == StepEnum.UploadMangadex)
                publishSteps.Add(StepEnum.ZipFiles);
        }

        return [.. publishSteps.Distinct().OrderBy(x => x)];
    }

    public Result ValidateStepsDependencies()
    {
        const string STEPS_KEY = "Settings:Publish:Steps";
        var configurationStepsAsString = configuration.GetRequiredValues<string>(STEPS_KEY, value => value);
        if (configurationStepsAsString.Length == 0)
            return Result.Fail($"Não foi encontrado nenhum passo de publicação em '{STEPS_KEY}'.");

        foreach (var configurationStepAsString in configurationStepsAsString)
        {
            if (!Enum.TryParse(typeof(StepEnum), configurationStepAsString, out _))
                return Result.Fail($"Não foi possível converter o tipo '{configurationStepAsString}' em um passo de publicação válido.");
        }

        var configurationSteps = configurationStepsAsString.Select(x => Enum.Parse(typeof(StepEnum), x));
        var publishSteps = steps.Where(x => configurationSteps.Contains(x.StepName));
        var uploadStepsCount = publishSteps.Count(x => x.StepType == StepType.Upload);
        if (uploadStepsCount == 0)
            return Result.Fail($"Não foi encontrado nenhum passo de publicação para disponibilização de lançamentos em '{STEPS_KEY}'.");

        return Result.Ok();
    }
}

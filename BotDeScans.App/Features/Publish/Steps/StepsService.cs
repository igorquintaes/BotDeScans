using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsService(IConfiguration configuration)
{
    public virtual Result ValidateStepsDependencies()
    {
        const string STEPS_KEY = "Settings:Publish:Steps";
        var configurationStepsAsString = configuration.GetRequiredValues<string>(STEPS_KEY, value => value);
        if (configurationStepsAsString.Length == 0)
            return Result.Fail($"Não foi encontrado nenhum passo de publicação em '{STEPS_KEY}'.");

        var configurationSteps = new List<StepName>();
        foreach (var configurationStepAsString in configurationStepsAsString)
        {
            if (!Enum.TryParse(typeof(StepName), configurationStepAsString, out var configurationStep))
                return Result.Fail($"Não foi possível converter o tipo '{configurationStepAsString}' em um passo de publicação válido.");

            configurationSteps.Add((StepName)configurationStep);
        }

        return StepsInfo.StepNameType
              .Where(x => configurationSteps.Contains(x.Key))
              .Any(x => x.Value == StepType.Upload) is false
                ? Result.Fail($"Não foi encontrado nenhum passo de publicação para disponibilização de lançamentos em '{STEPS_KEY}'.")
                : Result.Ok();
    }

    public virtual IReadOnlyList<StepName> GetPublishSteps()
    {
        var configurationSteps = configuration
            .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum
            .Parse(typeof(StepName), value));

        var publishSteps = new List<StepName>()
        {
            StepName.Download,
            StepName.Compress,
        };

        foreach (var configurationStep in configurationSteps)
        {
            var step = StepsInfo.StepNameType[configurationStep];
            if (step == StepType.Management)
                continue;

            publishSteps.Add(configurationStep);

            if (configurationStep is
                StepName.UploadPdfBox or
                StepName.UploadPdfMega or
                StepName.UploadPdfGoogleDrive)
                publishSteps.Add(StepName.PdfFiles);

            else if (configurationStep is
                StepName.UploadZipBox or
                StepName.UploadZipMega or
                StepName.UploadZipGoogleDrive or
                StepName.UploadMangadex)
                publishSteps.Add(StepName.ZipFiles);
        }

        return [.. publishSteps.Distinct().OrderBy(x => x)];
    }
}

using BotDeScans.App.Extensions;
using FluentResults;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsService(IConfiguration configuration)
{
    private readonly StepEnum[] steps = configuration.GetRequiredValues<StepEnum>("Settings:Publish:Steps", value => Enum.Parse(typeof(StepEnum), value));

    public Result ValidateStepsDependencies()
    {
        var result = Result.Ok();

        if (!steps.Contains(StepEnum.Download))
            result.WithError($"Erro: o passo {nameof(StepEnum.Download)} é obrigatório.");

        foreach (var step in steps)
        {
            result.WithReason(step switch
            {
                StepEnum.UploadPdfBox or
                StepEnum.UploadPdfMega or
                StepEnum.UploadPdfGoogleDrive when steps.NotContains(StepEnum.PdfFiles)
                    => ErrorFromDependency(step, requiredSteps: StepEnum.PdfFiles),
                StepEnum.UploadZipBox or
                StepEnum.UploadZipMega or
                StepEnum.UploadZipGoogleDrive or
                StepEnum.UploadMangadex or
                StepEnum.PublishBlogspot when steps.NotContainsAll(requiredBloggerSteps)
                    => ErrorFromDependency(step, requiredSteps: requiredBloggerSteps),
                _ => new Success("No error in current step! :)")
            });
        }

        return result;
    }

    static Error ErrorFromDependency(StepEnum step, params StepEnum[] requiredSteps)
    {
        var requiredStepAsString = string.Join(", ", requiredSteps.Select(x => x.ToString()));
        var errorMessageTitle = $"Erro no passo {step}";
        var errorMessageDescription = requiredSteps.Length == 1
            ? $"É obrigatório adicionar o passo {requiredStepAsString}."
            : $"É obrigatório adicionar um dos seguintes passos: {requiredStepAsString}.";

        return new Error($"{errorMessageTitle} - {errorMessageDescription}");
    }

    private static readonly StepEnum[] requiredBloggerSteps =
    [
        StepEnum.UploadPdfBox,
        StepEnum.UploadZipBox,
        StepEnum.UploadPdfMega,
        StepEnum.UploadZipMega,
        StepEnum.UploadPdfGoogleDrive,
        StepEnum.UploadZipGoogleDrive,
        StepEnum.UploadMangadex
    ];
}

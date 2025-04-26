using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Features.Publish.Steps.Models;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish;

public class PublishService(IConfiguration configuration, IEnumerable<IStep> steps)
{
    public virtual Steps.Models.Steps GetPublishStepsNames()
    {
        var configurationSteps = configuration
            .GetRequiredValues<StepName>("Settings:Publish:Steps", value => Enum
            .Parse(typeof(StepName), value));

        var publishStepsNames = new List<StepName>()
        {
            StepName.Download,
            StepName.Compress,
        };

        foreach (var configurationStep in configurationSteps)
        {
            publishStepsNames.Add(configurationStep);

            if (configurationStep is
                StepName.UploadPdfBox or
                StepName.UploadPdfMega or
                StepName.UploadPdfGoogleDrive)
                publishStepsNames.Add(StepName.PdfFiles);

            else if (configurationStep is
                StepName.UploadZipBox or
                StepName.UploadZipMega or
                StepName.UploadZipGoogleDrive or
                StepName.UploadMangadex)
                publishStepsNames.Add(StepName.ZipFiles);
        }

        return new Steps.Models.Steps(steps
            .Where(step => publishStepsNames.Contains(step.Name))
            .OrderBy(step => step.Name)
            .ToDictionary(step => step, step => new StepInfo(step)));
    }
}

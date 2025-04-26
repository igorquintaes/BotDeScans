using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using System.Collections.ObjectModel;
using System.Drawing;
namespace BotDeScans.App.Features.Publish.Steps.Models;

public class Steps(Dictionary<IStep, StepInfo> steps) : ReadOnlyDictionary<IStep, StepInfo>(steps)
{
    public static implicit operator Steps(IStep[] steps) =>
        new(steps.ToDictionary(step => step, step => new StepInfo(step)));

    public IEnumerable<(ManagementStep Step, StepInfo Info)> ManagementSteps =>
        this.Where(step => step.Key is ManagementStep)
            .Select(step => ((ManagementStep)step.Key, step.Value));

    public IEnumerable<(PublishStep Step, StepInfo Info)> PublishSteps =>
        this.Where(step => step.Key is PublishStep)
            .Select(step => ((PublishStep)step.Key, step.Value));

    public StepStatus Status =>
        this.All(x => x.Value.StepStatus == StepStatus.Success)
            ? StepStatus.Success
            : this.Any(x => x.Value.StepStatus == StepStatus.Error)
                ? StepStatus.Error
                : StepStatus.QueuedForExecution;

    public Color ColorStatus => Status switch
    {
        StepStatus.Success => Color.Green,
        StepStatus.Error => Color.Red,
        StepStatus.QueuedForExecution => Color.LightBlue,
        _ => throw new ArgumentOutOfRangeException(nameof(Status), $"Not expected Status value: {Status}")
    };

    public string Header => Status switch
    {
        StepStatus.Success => "Executado com sucesso!",
        StepStatus.Error => "Ocorreu um erro na execução!",
        StepStatus.QueuedForExecution => "Processando...",
        _ => throw new ArgumentOutOfRangeException(nameof(Status), $"Not expected Status value: {Status}")
    };

    public string Details => string.Join(
        Environment.NewLine,
        this.Select(task => $"{task.Value.StepStatus.GetEmoji()} - {task.Key.Name.GetDescription()}"));

    public static readonly IReadOnlyDictionary<StepName, StepType> StepNameType =
        new Dictionary<StepName, StepType>
        {
            { StepName.Download, StepType.Management },
            { StepName.Compress, StepType.Management },
            { StepName.ZipFiles, StepType.Management },
            { StepName.PdfFiles, StepType.Management },
            { StepName.UploadZipBox, StepType.Publish },
            { StepName.UploadPdfBox, StepType.Publish },
            { StepName.UploadZipGoogleDrive, StepType.Publish },
            { StepName.UploadPdfGoogleDrive, StepType.Publish },
            { StepName.UploadZipMega, StepType.Publish },
            { StepName.UploadPdfMega, StepType.Publish },
            { StepName.UploadMangadex, StepType.Publish },
            { StepName.PublishBlogspot, StepType.Publish }
        };
}
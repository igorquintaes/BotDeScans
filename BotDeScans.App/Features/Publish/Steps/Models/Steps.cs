using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using System.Collections.ObjectModel;
using System.Drawing;
namespace BotDeScans.App.Features.Publish.Steps.Models;

public class Steps(Dictionary<IStep, StepInfo> steps) : ReadOnlyDictionary<IStep, StepInfo>(steps)
{
    public IEnumerable<(IManagementStep Step, StepInfo Info)> ManagementSteps =>
        this.Where(step => step.Key is IManagementStep)
            .Select(step => ((IManagementStep)step.Key, step.Value));

    public IEnumerable<(IPublishStep Step, StepInfo Info)> PublishSteps =>
        this.Where(step => step.Key is IPublishStep)
            .Select(step => ((IPublishStep)step.Key, step.Value));

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
            { StepName.UploadZipBox, StepType.Upload },
            { StepName.UploadPdfBox, StepType.Upload },
            { StepName.UploadZipGoogleDrive, StepType.Upload },
            { StepName.UploadPdfGoogleDrive, StepType.Upload },
            { StepName.UploadZipMega, StepType.Upload },
            { StepName.UploadPdfMega, StepType.Upload },
            { StepName.UploadMangadex, StepType.Upload },
            { StepName.PublishBlogspot, StepType.Publish }
        };
}
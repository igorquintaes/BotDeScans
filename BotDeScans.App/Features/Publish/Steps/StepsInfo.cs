using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps.Enums;
using System.Drawing;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsInfo(IDictionary<StepName, StepStatus> steps) : Dictionary<StepName, StepStatus>(steps)
{
    public static implicit operator StepsInfo((StepName, StepStatus)[] steps) =>
        new(steps.ToDictionary(x => x.Item1, x => x.Item2));

    public StepStatus Status =>
        this.All(x => x.Value == StepStatus.Success)
            ? StepStatus.Success
            : this.Any(x => x.Value == StepStatus.Error)
                ? StepStatus.Error
                : StepStatus.Executing;

    public Color ColorStatus => Status switch
    {
        StepStatus.Success => Color.Green,
        StepStatus.Error => Color.Red,
        StepStatus.Executing => Color.LightBlue,
        _ => throw new ArgumentOutOfRangeException(nameof(Status), $"Not expected Status value: {Status}")
    };

    public string Header => Status switch
    {
        StepStatus.Success => "Executado com sucesso!",
        StepStatus.Error => "Ocorreu um erro na execução!",
        StepStatus.Executing => "Processando...",
        _ => throw new ArgumentOutOfRangeException(nameof(Status), $"Not expected Status value: {Status}")
    };

    public string Details => string.Join(
        Environment.NewLine,
        this.Select(task => $"{task.Value.GetEmoji()} - {task.Key.GetDescription()}"));

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
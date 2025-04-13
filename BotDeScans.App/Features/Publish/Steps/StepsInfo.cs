using BotDeScans.App.Attributes;
using BotDeScans.App.Extensions;
using System.ComponentModel;
using System.Drawing;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsInfo(IDictionary<StepEnum, StepStatus> steps) : Dictionary<StepEnum, StepStatus>(steps)
{
    public static implicit operator StepsInfo((StepEnum, StepStatus)[] steps) => 
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

    public static readonly IReadOnlyDictionary<StepEnum, StepType> StepEnumType = 
        new Dictionary<StepEnum, StepType>
        {
            { StepEnum.Download, StepType.Management },
            { StepEnum.Compress, StepType.Management },
            { StepEnum.ZipFiles, StepType.Management },
            { StepEnum.PdfFiles, StepType.Management },
            { StepEnum.UploadZipBox, StepType.Upload },
            { StepEnum.UploadPdfBox, StepType.Upload },
            { StepEnum.UploadZipGoogleDrive, StepType.Upload },
            { StepEnum.UploadPdfGoogleDrive, StepType.Upload },
            { StepEnum.UploadZipMega, StepType.Upload },
            { StepEnum.UploadPdfMega, StepType.Upload },
            { StepEnum.UploadMangadex, StepType.Upload },
            { StepEnum.PublishBlogspot, StepType.Publish }
        };
}

public enum StepStatus
{
    [Emoji("clock10")]
    Queued,
    [Emoji("fire")]
    Executing,
    [Emoji("white_check_mark")]
    Success,
    [Emoji("warning")]
    Error,
    [Emoji("sos")]
    Fatal
}

public enum StepEnum
{
    [Description("Baixar")]
    Download,
    [Description("Compressão")]
    Compress,
    [Description("Transformar em zip")]
    ZipFiles,
    [Description("Transformar em pdf")]
    PdfFiles,
    [Description("Hospedar zip - Mega")]
    UploadZipMega,
    [Description("Hospedar pdf - Mega")]
    UploadPdfMega,
    [Description("Hospedar zip - Box")]
    UploadZipBox,
    [Description("Hospedar pdf - Box")]
    UploadPdfBox,
    [Description("Hospedar zip - Google Drive")]
    UploadZipGoogleDrive,
    [Description("Hospedar pdf - Google Drive")]
    UploadPdfGoogleDrive,
    [Description("Publicar na Mangadex")]
    UploadMangadex,
    [Description("Publicar no Blogspot")]
    PublishBlogspot
}

public enum StepType
{
    Management,
    Upload,
    Publish
}

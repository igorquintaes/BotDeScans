using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Runtime.Serialization;
namespace BotDeScans.App.Services.Publish;

public class PublishState
{
    public PublishState(IConfiguration configuration)
    {
        var stepsAsString = configuration.GetRequiredValues<StepEnum>(
            "Settings:Publish:Steps", 
            value => Enum.Parse(typeof(StepEnum), value));

        Steps = Enum
            .GetValues<StepEnum>()
            .Select(@enum => Array.Exists(stepsAsString, stepEnum => stepEnum == @enum)
                ? new KeyValuePair<StepEnum, StepStatus>(@enum, StepStatus.Queued)
                : new KeyValuePair<StepEnum, StepStatus>(@enum, StepStatus.Skip))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public IDictionary<StepEnum, StepStatus> Steps { get; set; }
    public ReleaseInfo Info { get; set; } = new ReleaseInfo();
    public ReleaseLinks Links { get; set; } = new ReleaseLinks();
    public ReleaseInternalData InternalData { get; set; } = new ReleaseInternalData();

    public record ReleaseInternalData
    {
        public string OriginContentFolder { get; set; } = null!;
        public string CoverFilePath { get; set; } = null!;
        public string ZipFilePath { get; set; } = null!;
        public string PdfFilePath { get; set; } = null!;
    }

    public record ReleaseInfo
    {
        public string Link { get; set; } = null!;
        public string DisplayTitle { get; set; } = null!;
        public string? ChapterName { get; set; }
        public string ChapterNumber { get; set; } = null!;
        public string? ChapterVolume { get; set; }
        public string? Message { get; set; }
    }

    public record ReleaseLinks
    {

        [Description("Mega [Zip]")]
        public string? MegaZip { get; set; }

        [Description("Mega [Pdf]")]
        public string? MegaPdf { get; set; }

        [Description("Google Drive [Zip]")]
        public string? DriveZip { get; set; }

        [Description("Google Drive [Pdf]")]
        public string? DrivePdf { get; set; }

        [Description("Box [Zip]")]
        public string? BoxZip { get; set; }

        [Description("Box [Pdf]")]
        public string? BoxPdf { get; set; }

        [Description("Em nosso site")]
        public string? BloggerLink { get; set; }

        [Description("Online [MangaDex]")]
        public string? MangaDexLink { get; set; }

        [Description("Online [Tsuki]")]
        public string? TsukiLink { get; set; }

        public string? BoxPdfReader { get; set; }
    }
}

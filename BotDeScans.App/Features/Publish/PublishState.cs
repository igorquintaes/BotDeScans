using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Models;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
namespace BotDeScans.App.Features.Publish;

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
    public Title Title { get; set; } = null!;
    public Info ReleaseInfo { get; set; } = new Info();
    public Links ReleaseLinks { get; set; } = new Links();
    public ReleaseInternalData InternalData { get; set; } = new ReleaseInternalData();

    public record ReleaseInternalData
    {
        public string OriginContentFolder { get; set; } = null!;
        public string GoogleDriveFolderId { get; set; } = null!;
        public string CoverFilePath { get; set; } = null!;
        public string ZipFilePath { get; set; } = null!;
        public string PdfFilePath { get; set; } = null!;
    }

    public record Info
    {
        public string DownloadUrl { get; set; } = null!;
        public string? ChapterName { get; set; }
        public string ChapterNumber { get; set; } = null!;
        public string? ChapterVolume { get; set; }
        public string? Message { get; set; }
    }

    public record Links
    {
        [Description("Mega [Zip]")]
        public string? MegaZip { get; set; }

        [Description("Mega [Pdf]")]
        public string? MegaPdf { get; set; }

        [Description("Drive [Zip]")]
        public string? DriveZip { get; set; }

        [Description("Drive [Pdf]")]
        public string? DrivePdf { get; set; }

        [Description("Box [Zip]")]
        public string? BoxZip { get; set; }

        [Description("Box [Pdf]")]
        public string? BoxPdf { get; set; }

        [Description("Blogger")]
        public string? BloggerLink { get; set; }

        [Description("MangaDex")]
        public string? MangaDexLink { get; set; }

        [Description("SlimeRead")]
        public string? SlimeReadLink { get; set; }

        public string? BoxPdfReaderKey { get; set; }
    }
}

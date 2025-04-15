using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using BotDeScans.App.Models;
using System.ComponentModel;
namespace BotDeScans.App.Features.Publish;

public class PublishState(StepsService stepsService)
{
    public StepsInfo? Steps { get; private set; }
    public Title Title { get; set; } = null!;
    public Info ReleaseInfo { get; set; } = null!;
    public Links ReleaseLinks { get; set; } = new Links();
    public ReleaseInternalData InternalData { get; set; } = new ReleaseInternalData();

    public virtual void LoadSteps() => 
        Steps = stepsService
            .GetPublishSteps()
            .Select(x => (x, StepStatus.Queued))
            .ToArray();

    public record ReleaseInternalData
    {
        public string OriginContentFolder { get; set; } = null!;
        public string GoogleDriveFolderId { get; set; } = null!;
        public string CoverFilePath { get; set; } = null!;
        public string? ZipFilePath { get; set; }
        public string? PdfFilePath { get; set; }
        public string? BloggerImageAsBase64 { get; set; }
    }

    public record Info
    {
        public string DownloadUrl { get; init; }
        public string? ChapterName { get; init; }
        public string ChapterNumber { get; init; }
        public string? ChapterVolume { get; init; }
        public string? Message { get; init; }
        public int TitleId { get; init; }


        public Info(
            string downloadUrl,
            string? chapterName,
            string chapterNumber,
            string? chapterVolume,
            string? message,
            int titleId)
        {
            DownloadUrl = downloadUrl;
            ChapterName = chapterName.NullIfWhitespace();
            ChapterNumber = chapterNumber;
            ChapterVolume = chapterVolume.NullIfWhitespace();
            Message = message.NullIfWhitespace();
            TitleId = titleId;
        }

        public override string ToString() => 
            $"DownloadUrl: {DownloadUrl}{Environment.NewLine}" +
            $"ChapterName: {ChapterName}{Environment.NewLine}" +
            $"ChapterNumber: {ChapterNumber}{Environment.NewLine}" +
            $"ChapterVolume: {ChapterVolume}{Environment.NewLine}" +
            $"Message: {Message}{Environment.NewLine}";
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

        public string? BoxPdfReaderKey { get; set; }
    }
}

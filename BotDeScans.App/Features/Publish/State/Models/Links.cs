using System.ComponentModel;
namespace BotDeScans.App.Features.Publish.State.Models;

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

    [Description("MangaDex")]
    public string? MangaDexLink { get; set; }

    [Description("Blogger")]
    public string? BloggerLink { get; set; }
}

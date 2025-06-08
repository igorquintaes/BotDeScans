using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

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
    public string? MangaDex { get; set; }

    [Description("Sakura Mangás")]
    public string? SakuraMangas { get; set; }

    [Description("Blogger")]
    public string? Blogger { get; set; }
}

using System.ComponentModel;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

public record Links
{
    [Description("Mega [Zip]")]
    public string? MegaZip { get; init; }

    [Description("Mega [Pdf]")]
    public string? MegaPdf { get; init; }

    [Description("Drive [Zip]")]
    public string? DriveZip { get; init; }

    [Description("Drive [Pdf]")]
    public string? DrivePdf { get; init; }

    [Description("Box [Zip]")]
    public string? BoxZip { get; init; }

    [Description("Box [Pdf]")]
    public string? BoxPdf { get; init; }

    [Description("MangaDex")]
    public string? MangaDex { get; init; }

    [Description("Sakura Mangás")]
    public string? SakuraMangas { get; init; }

    [Description("Blogger")]
    public string? Blogger { get; init; }
}

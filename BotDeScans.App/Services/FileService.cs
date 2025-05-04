using FluentResults;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO.Compression;
using Path = System.IO.Path;
namespace BotDeScans.App.Services;

public class FileService
{
    public static readonly IDictionary<string, string> MimeTypes =
        new Dictionary<string, string>
        {
            { ".jpeg", "image/jpeg" },
            { ".jpg", "image/jpeg" },
            { ".png", "image/png" },
            { ".zip", "application/zip" },
            { ".pdf", "application/pdf" }
        };

    public virtual string GetMimeType(string fileName) =>
        MimeTypes[Path.GetExtension(fileName)];

    // Limitação assíncrona: https://github.com/dotnet/runtime/issues/1541
    public virtual Result<string> CreateZipFile(
        string fileName,
        string resourcesDirectory,
        string destinationDirectory)
    {
        if (resourcesDirectory.Equals(destinationDirectory, StringComparison.InvariantCultureIgnoreCase))
            return Result.Fail("Source and destination directories should not be the same.");

        var pages = Directory.GetFiles(resourcesDirectory).OrderBy(x => x).ToArray();
        var pagesQuantity = Math.Floor(Math.Log10(pages.Length) + 1);
        var filePath = Path.Combine(destinationDirectory, $"{fileName}.zip");
        using var newFile = ZipFile.Open(filePath, ZipArchiveMode.Create);
        for (var i = 0; i < pages.Length; i++)
        {
            var pageNumber = (i+1).ToString("D" + pagesQuantity);
            newFile.CreateEntryFromFile(pages[i], pageNumber + Path.GetExtension(pages[i]), CompressionLevel.SmallestSize);
        }

        return filePath;
    }

    public virtual async Task<Result<string>> CreatePdfFileAsync(
        string fileName,
        string resourcesDirectory,
        string destinationDirectory)
    {
        if (resourcesDirectory.Equals(destinationDirectory, StringComparison.InvariantCultureIgnoreCase))
            return Result.Fail("Source and destination directories should not be the same.");

        var filePath = Path.Combine(destinationDirectory, $"{fileName}.pdf");
        await using var pdfWritter = new PdfWriter(filePath);
        using var pdf = new PdfDocument(pdfWritter);
        using var document = new Document(pdf);
        document.SetMargins(0, 0, 0, 0);

        string[] allowedExtensions = [".png", ".jpg", ".jpeg"];
        foreach (var imagePath in Directory.GetFiles(resourcesDirectory)
            .Where(file => allowedExtensions
            .Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
        {
            var image = new Image(ImageDataFactory.Create(imagePath));
            pdf.SetDefaultPageSize(new PageSize(image.GetImageWidth(), image.GetImageHeight()));
            document.Add(image);
        }

        document.Close();

        return filePath;
    }
}

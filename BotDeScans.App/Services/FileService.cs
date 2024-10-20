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

    // TODO: Limitação assíncrona: https://github.com/dotnet/runtime/issues/1541
    // Talvez seja resolvida no .NET 8 pela microsoft
    // EDIT 1: Não foi. Vai ser implementado quando eu morrer
    public string CreateZipFile(
        string fileName,
        string resourcesDirectory,
        string destinationDirectory)
    {
        if (!fileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("Desired zip file name must has .zip extension.");

        if (resourcesDirectory.Equals(destinationDirectory, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("Source and destination directories should not be the same.");

        var filePath = Path.Combine(destinationDirectory, fileName);
        using var newFile = ZipFile.Open(filePath, ZipArchiveMode.Create);
        foreach (var file in Directory.GetFiles(resourcesDirectory))
            newFile.CreateEntryFromFile(
                file,
                Path.GetFileName(file),
                CompressionLevel.SmallestSize);

        return filePath;
    }

    public async Task<string> CreatePdfFileAsync(
        string fileName,
        string resourcesDirectory,
        string destinationDirectory)
    {
        // todo: verificar possibilidade de pdf sem itext
        if (!fileName.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("Desired zip file name must has .zip extension.");

        if (resourcesDirectory.Equals(destinationDirectory, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("Source and destination directories should not be the same.");

        var filePath = Path.Combine(destinationDirectory, fileName);
        await using var pdfWritter = new PdfWriter(filePath);
        using var pdf = new PdfDocument(pdfWritter);
        using var document = new Document(pdf);
        document.SetMargins(0, 0, 0, 0);

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
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

    /// <summary>
    /// Creates a file
    /// </summary>
    /// <param name="fileBytes">File content as byte[]</param>
    /// <param name="fileName">File full path to be created</param>
    /// <exception cref="System.IO.DirectoryNotFoundException">Throws if target file directory does not exists.</exception>
    public virtual async Task CreateFileAsync(byte[] fileBytes, string fileName)
    {
        await using var stream = File.Create(fileName);
        await stream.WriteAsync(fileBytes);
    }


}

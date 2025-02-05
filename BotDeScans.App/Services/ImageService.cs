using Imageflow.Fluent;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
namespace BotDeScans.App.Services;

public class ImageService(IConfiguration configuration)
{
    public virtual async Task CompressImageAsync(
        string filePath,
        bool isGrayscale,
        CancellationToken cancellationToken)
    {
        var (quality, minQuality) = GetImageQuality(isGrayscale);
        var imageBytes = await TryCompressAsync(filePath, quality, minQuality);
        if (imageBytes == null)
            return;

        using var stream = File.Create(Path.ChangeExtension(filePath, ".png"));
        await stream.WriteAsync(imageBytes.Value, cancellationToken);

        if (Path.GetExtension(filePath) != ".png")
            File.Delete(filePath);
    }

    public virtual async Task<string> CreateBase64String(
        string filePath,
        int maxWidth,
        int maxHeight,
        bool isGrayscale,
        CancellationToken cancellationToken)
    {
        var command = $"width={maxWidth}&height={maxHeight}&mode=max&scale=both";
        var (quality, minQuality) = GetImageQuality(isGrayscale);
        var imageBytes = await TryCompressAsync(filePath, quality, minQuality, command);

        return Convert.ToBase64String(imageBytes ?? throw new Exception("Unable to convert image to base64"));
    }

    /// <summary>
    /// Source: https://stackoverflow.com/a/62961179
    /// Determine if an image is greyscale
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="threshold">Color variation to ignore</param>
    /// <returns></returns>
    public virtual bool IsGrayscale(string filePath, int threshold = 20)
    {
        //Load image
        using var fileStream = File.OpenRead(filePath);
        using var image = Image.Load<Rgba32>(fileStream);
        foreach (var row in image.GetPixelMemoryGroup())
        foreach (var pixel in row.Span)
        {
            if (pixel.A == 0) //ignore fully transparent pixels 
                continue;

            if (GetRgbDelta(pixel.R, pixel.G, pixel.B) > threshold)
                return false;
        }

        return true;

        static int GetRgbDelta(byte r, byte g, byte b)
            => Math.Abs(r - g) +
               Math.Abs(g - b) +
               Math.Abs(b - r);

    }

    private static async Task<ArraySegment<byte>?> TryCompressAsync(
        string filePath,
        int quality,
        int minQuality,
        string resizerCommands = "")
    {
        using var fileStream = File.OpenRead(filePath);
        using var imageJob = new ImageJob();
        var imageJobResult = await imageJob
            .Decode(source: BufferedStreamSource.UseEntireStreamAndDisposeWithSource(fileStream),
                    commands: new DecodeCommands().SetIgnoreColorProfileErrors(true))
            .ResizerCommands(resizerCommands)
            .EncodeToBytes(new PngQuantEncoder(quality, minQuality))
            .Finish()
            .InProcessAsync();

        return imageJobResult.First?.TryGetBytes();
    }

    private (int quality, int minQuality) GetImageQuality(bool isGrayscale)
        => isGrayscale
            ? (configuration.GetValue<int?>("Compress:Grayscale:Quality") ?? 50,
               configuration.GetValue<int?>("Compress:Grayscale:MinimumQuality") ?? 30)
            : (configuration.GetValue<int?>("Compress:Colorful:Quality") ?? 90,
               configuration.GetValue<int?>("Compress:Colorful:MinimumQuality") ?? 85);
}

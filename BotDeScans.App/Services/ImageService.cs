using Imageflow.Fluent;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
namespace BotDeScans.App.Services;

public class ImageService(IConfiguration configuration)
{
    public async Task CompressImagesAsync(string directory, CancellationToken cancellationToken = default)
    {
        var maxDegreeOfParallelism = configuration.GetValue<int?>("Compress:ParallelismDegree")
            ?? Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0));

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = maxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(
            Directory.GetFiles(directory),
            parallelOptions,
            async (filePath, ct) =>
        {
            var (quality, minQuality) = GetImageQuality(filePath);

            using var imageJob = new ImageJob();
            await using var fileStream = File.OpenRead(filePath);
            var imageJobResult = await imageJob
                .Decode(source: BufferedStreamSource.UseEntireStreamAndDisposeWithSource(fileStream),
                        commands: new DecodeCommands().SetIgnoreColorProfileErrors(true))
                .EncodeToBytes(new PngQuantEncoder(quality, minQuality))
                .Finish()
                .InProcessAsync();

            var bytes = imageJobResult.First?.TryGetBytes();
            if (bytes != null)
            {
                var newFileName = $"{Path.GetFileNameWithoutExtension(filePath)}.png";
                using var stream = File.Create(Path.Combine(directory, newFileName));
                await stream.WriteAsync(bytes.Value, ct);
            }

            if (Path.GetExtension(filePath) != ".png")
                File.Delete(filePath);
        });
    }

    public async Task<string> CreateBase64File(
        string filePath,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken)
    {
        var (quality, minQuality) = GetImageQuality(filePath);

        using var imageJob = new ImageJob();
        var imageBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var imageJobResult = await imageJob
            .Decode(imageBytes)
            .ResizerCommands($"width={maxWidth}&height={maxHeight}&mode=max&scale=both")
            .EncodeToBytes(new PngQuantEncoder(quality, minQuality))
            .Finish()
            .InProcessAsync();

        var convertedImageInBytes = imageJobResult.First?.TryGetBytes()
            ?? throw new Exception("Unable to convert image");

        return Convert.ToBase64String(convertedImageInBytes);
    }

    private (int quality, int minQuality) GetImageQuality(string filePath)
        => IsGrayscale(filePath, 10)
            ? (configuration.GetValue<int?>("Compress:Grayscale:Quality") ?? 50,
               configuration.GetValue<int?>("Compress:Grayscale:MinimumQuality") ?? 30)
            : (configuration.GetValue<int?>("Compress:Colorful:Quality") ?? 90,
               configuration.GetValue<int?>("Compress:Colorful:MinimumQuality") ?? 85);

    /// <summary>
    /// Source: https://stackoverflow.com/a/62961179
    /// Determine if an image is greyscale
    /// </summary>
    /// <param name="filePath">The path to the image file.</param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    private static bool IsGrayscale(string filePath, int threshold)
    {
        //Load image
        using var image = Image.Load<Rgba32>(filePath);


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
}

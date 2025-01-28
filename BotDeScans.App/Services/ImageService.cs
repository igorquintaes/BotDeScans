using Imageflow.Fluent;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
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
    {
        using var bitmap = new Bitmap(filePath);
        return GetImageQuality(bitmap);
    }

    private (int quality, int minQuality) GetImageQuality(Bitmap bitmap)
        => IsGrayscale(bitmap, 10)
            ? (configuration.GetValue<int?>("Compress:Grayscale:Quality") ?? 50,
               configuration.GetValue<int?>("Compress:Grayscale:MinimumQuality") ?? 30)
            : (configuration.GetValue<int?>("Compress:Colorful:Quality") ?? 90,
               configuration.GetValue<int?>("Compress:Colorful:MinimumQuality") ?? 85);

    /// <summary>
    /// Source: https://stackoverflow.com/a/1877420
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    private static bool IsGrayscale(Bitmap bitmap, int threshold)
    {
        // Indexed format, and no non-gray colours in the images palette: immediate pass.
        if ((bitmap.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed
            && bitmap.Palette.Entries.All(c => c.R == c.G && c.R == c.B))
            return true;

        // Quick indexed check failed; actually check image data.
        // Get bytes out of the image, converted to 32bpp ARGB 
        var curBitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        var stride = curBitmapData.Stride;
        var data = new byte[stride * bitmap.Height];
        Marshal.Copy(curBitmapData.Scan0, data, 0, data.Length);
        bitmap.UnlockBits(curBitmapData);
        // Go over all bytes per block of four.
        var curRowOffs = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            // Set offset to start of current row
            var curOffs = curRowOffs;
            for (var x = 0; x < bitmap.Width; x++)
            {
                var b = data[curOffs];
                var g = data[curOffs + 1];
                var r = data[curOffs + 2];
                var a = data[curOffs + 3];

                // Increase offset to next colour
                curOffs += 4;

                if (a == 0)
                    continue;
                if (GetRgbDelta(r, g, b) > threshold)
                    return false;
            }
            // Increase row offset
            curRowOffs += stride;
        }

        static int GetRgbDelta(byte r, byte g, byte b)
            => Math.Abs(r - g) +
               Math.Abs(g - b) +
               Math.Abs(b - r);

        return true;
    }
}

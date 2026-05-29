using BotDeScans.App.Services;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services;

public class ImageServiceTests : UnitTest, IDisposable
{
    private readonly ImageService service;

    private static readonly string imagePath = Path.Combine(
        path1: Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        path2: "test.png");

    public ImageServiceTests()
    {
        fixture.FreezeFake<IConfiguration>();
        service = fixture.Create<ImageService>();
    }

    public class IsGrayscale : ImageServiceTests
    {
        private static readonly SKColor grey = new SKColor(5, 5, 5);
        private static readonly SKColor notGrey = new SKColor(5, 5, 6);

        [Fact]
        public void GivenGreyImageShouldReturnTrue()
        {
            SaveImage(imagePath, 1, 1, grey);

            service.IsGrayscale(imagePath, threshold: 0)
                   .Should().BeTrue();
        }

        [Fact]
        public void GivenNotAGreyImageShouldReturnFalse()
        {
            SaveImage(imagePath, 1, 1, notGrey);

            service.IsGrayscale(imagePath, threshold: 0)
                   .Should().BeFalse();
        }

        [Fact]
        public void GivenNotAGreyImageButInsideThresholdDifferenteShouldReturnTrue()
        {
            SaveImage(imagePath, 1, 1, notGrey);

            service.IsGrayscale(imagePath, threshold: 2)
                   .Should().BeTrue();
        }

        [Fact]
        public void GivenImageWithGreyAndColouredPixelsShouldReturnFalse()
        {
            using (var bitmap = new SKBitmap(2, 1))
            {
                bitmap.SetPixel(0, 0, grey);
                bitmap.SetPixel(1, 0, notGrey);
                SaveBitmap(imagePath, bitmap);
            }

            service.IsGrayscale(imagePath, threshold: 0)
                   .Should().BeFalse();
        }
    }

    public class CreateBase64StringAsync : ImageServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GivenImageShouldGenerateBase64ImageAsExpected(bool isGrayScale)
        {
            const string expectedResult = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAMAAAAoyzS7AAAAA1BMVEUFBQWsrP/7AAAACklEQVR4nGNgAAAAAgABSK+kcQAAAABJRU5ErkJggg==";
            SaveImage(imagePath, 1, 1, new SKColor(5, 5, 5));

            var result = await service.CreateBase64StringAsync(imagePath, 1, 1, isGrayScale, cancellationToken);
            result.Should().Be(expectedResult);
        }
    }

    public class CompressImageAsync : ImageServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GivenImageShouldCompress(bool isGrayScale)
        {
            SaveImage(imagePath, 100, 100, new SKColor(5, 5, 5));

            var oldFileBytes = File.ReadAllBytes(imagePath);

            await service.CompressImageAsync(imagePath, isGrayScale, cancellationToken);
            var newFileBytes = File.ReadAllBytes(imagePath);

            oldFileBytes.Length.Should().BeGreaterThan(newFileBytes.Length);
        }

        [Fact]
        public async Task GivenNotAPngImageShouldDeleteOriginalFile()
        {
            var jpgImagePath = Path.ChangeExtension(imagePath, ".jpg");
            SaveImage(jpgImagePath, 1, 1, new SKColor(5, 5, 5));

            await service.CompressImageAsync(jpgImagePath, default, cancellationToken);

            File.Exists(jpgImagePath).Should().BeFalse();
        }
    }

    private static void SaveImage(string path, int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        SaveBitmap(path, bitmap);
    }

    private static void SaveBitmap(string path, SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    public void Dispose()
    {
        if (File.Exists(imagePath))
            File.Delete(imagePath);

        GC.SuppressFinalize(this);
    }
}

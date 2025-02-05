using AutoFixture;
using BotDeScans.App.Services;
using BotDeScans.UnitTests.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using static BotDeScans.UnitTests.Specs.Services.ImageServiceTests;

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
        private static readonly Color grey = new Rgba32(5, 5, 5);
        private static readonly Color notGrey = new Rgba32(5, 5, 6);

        [Fact]
        public async Task GivenGreyImageShouldReturnTrue()
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Mutate(x => x.BackgroundColor(grey));
                await image.SaveAsync(imagePath, cancellationToken);
            }

            service.IsGrayscale(imagePath, threshold: 0)
                   .Should().BeTrue();
        }

        [Fact]
        public async Task GivenNotAGreyImageShouldReturnFalse()
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Mutate(x => x.BackgroundColor(notGrey));
                await image.SaveAsync(imagePath, cancellationToken);
            }

            service.IsGrayscale(imagePath, threshold: 0)
                   .Should().BeFalse();
        }

        [Fact]
        public async Task GivenNotAGreyImageButInsideThresholdDifferenteShouldReturnTrue()
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Mutate(x => x.BackgroundColor(notGrey));
                await image.SaveAsync(imagePath, cancellationToken);
            }

            service.IsGrayscale(imagePath, threshold: 2)
                   .Should().BeTrue();
        }

        [Fact]
        public async Task GivenImageWithGreyAndColouredPixelsShouldReturnFalse()
        {
            using (var image = new Image<Rgba32>(2, 1))
            {
                image.Mutate(x => x.BackgroundColor(grey, new Rectangle(0, 0, 1, 1)));
                image.Mutate(x => x.BackgroundColor(notGrey, new Rectangle(1, 0, 1, 1)));
                await image.SaveAsync(imagePath, cancellationToken);
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
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Mutate(x => x.BackgroundColor(new Rgba32(5, 5, 5)));
                await image.SaveAsync(imagePath, cancellationToken);
            }

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
            using (var image = new Image<Rgba32>(100, 100))
            {
                image.Mutate(x => x.BackgroundColor(new Rgba32(5, 5, 5)));
                await image.SaveAsync(imagePath, cancellationToken);
            }

            var oldFileBytes = File.ReadAllBytes(imagePath);

            await service.CompressImageAsync(imagePath, isGrayScale, cancellationToken);
            var newFileBytes = File.ReadAllBytes(imagePath);

            oldFileBytes.Length.Should().BeGreaterThan(newFileBytes.Length);
        }

        [Fact]
        public async Task GivenNotAPngImageShouldDeleteOriginalFile()
        {
            var jpgImagePath = Path.ChangeExtension(imagePath, ".jpg");
            using (var image = new Image<Rgba32>(1, 1))
            {
                image.Mutate(x => x.BackgroundColor(new Rgba32(5, 5, 5)));
                await image.SaveAsync(jpgImagePath, cancellationToken);
            }

            await service.CompressImageAsync(jpgImagePath, default, cancellationToken);

            File.Exists(jpgImagePath).Should().BeFalse();
        }
    }

    public void Dispose()
    {
        if (File.Exists(imagePath))
            File.Delete(imagePath);

        GC.SuppressFinalize(this);
    }
}

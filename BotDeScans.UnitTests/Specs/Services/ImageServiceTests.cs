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

namespace BotDeScans.UnitTests.Specs.Services;

public class ImageServiceTests : UnitTest
{
    private readonly ImageService service;

    public ImageServiceTests()
    {
        fixture.FreezeFake<IConfiguration>();

        service = fixture.Create<ImageService>();
    }

    public class IsGrayscale : ImageServiceTests, IDisposable
    {
        private static readonly Color grey = new Rgba32(5, 5, 5);
        private static readonly Color notGrey = new Rgba32(5, 5, 6);
        private static readonly string imagePath = Path.Combine(
            path1: Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            path2: "test.png");

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

        public void Dispose()
        {
            if (File.Exists(imagePath))
                File.Delete(imagePath);

            GC.SuppressFinalize(this);
        }
    }
}

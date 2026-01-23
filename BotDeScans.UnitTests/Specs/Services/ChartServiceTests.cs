using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Globalization;
using System.Reflection;

namespace BotDeScans.UnitTests.Specs.Services;

public class ChartServiceTests : UnitTest
{
    public class CreatePieChart : ChartServiceTests
    {
        [Fact]
        public void ShouldCreatePieChartAsExpected()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var service = new ChartService();
            var data = new ConsumptionData(500, 100);
            using var resultChartStream = service.CreatePieChart(data);
            using var resultChartImage = Image.Load<Rgba32>(resultChartStream);

            var expectedImagePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "Resources",
                "chart.png");

            using var expectedChartImage = Image.Load<Rgba32>(expectedImagePath);

            resultChartImage.Width.Should().Be(expectedChartImage.Width);
            resultChartImage.Height.Should().Be(expectedChartImage.Height);

            // OS can render slightly different charts, so we allow a small tolerance
            var totalPixels = resultChartImage.Width * resultChartImage.Height;
            var differentPixels = 0;
            const int colorTolerance = 5; // Tolerante by color channel
            const double maxDifferencePercentage = 0.05; // 5% of pixels

            for (int x = 0; x < resultChartImage.Width; x++)
            {
                for (int y = 0; y < resultChartImage.Height; y++)
                {
                    var resultPixel = resultChartImage[x, y];
                    var expectedPixel = expectedChartImage[x, y];

                    if (!ArePixelsSimilar(resultPixel, expectedPixel, colorTolerance))
                    {
                        differentPixels++;
                    }
                }
            }

            var differencePercentage = (double)differentPixels / totalPixels;
            differencePercentage.Should().BeLessThan(maxDifferencePercentage,
                because: $"{differentPixels} pixels are different a total of {totalPixels} pixels ({differencePercentage:P2})");
        }

        private static bool ArePixelsSimilar(Rgba32 pixel1, Rgba32 pixel2, int tolerance)
        {
            return Math.Abs(pixel1.R - pixel2.R) <= tolerance &&
                   Math.Abs(pixel1.G - pixel2.G) <= tolerance &&
                   Math.Abs(pixel1.B - pixel2.B) <= tolerance &&
                   Math.Abs(pixel1.A - pixel2.A) <= tolerance;
        }
    }
}
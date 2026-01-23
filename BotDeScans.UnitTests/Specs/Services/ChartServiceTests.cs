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

            var equalImages =
                resultChartImage.Width == expectedChartImage.Width &&
                resultChartImage.Height == expectedChartImage.Height;

            if (equalImages)
            {
                for (int i = 0; i < resultChartImage.Width; i++)
                {
                    for (int j = 0; j < resultChartImage.Height; j++)
                    {
                        var resultPixel = resultChartImage[i, j];
                        var expectedPixel = expectedChartImage[i, j];

                        if (resultPixel != expectedPixel)
                        {
                            equalImages = false;
                            break;
                        }
                    }

                    if (!equalImages)
                        break;
                }
            }

            equalImages.Should().BeTrue();
        }
    }
}
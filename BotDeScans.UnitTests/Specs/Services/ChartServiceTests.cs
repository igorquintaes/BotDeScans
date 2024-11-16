using BotDeScans.App.Services;
using FluentAssertions;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
namespace BotDeScans.UnitTests.Specs.Services;

public class ChartServiceTests : UnitTest
{
    public class CreatePieChart : ChartServiceTests
    {
        [Fact(Skip = "comma format is not culture insensitive")]
        public void ShouldCreatePieChartAsExpected()
        {
            var service = new ChartService();
            var data = new Dictionary<string, double>
            {
                { "Some label", 500d },
                { "Other", 75.3d },
                { "Last", 10d },
            };

            using var resultChartStream = service.CreatePieChart(data);
            using var resultChartImage = (Bitmap)Image.FromStream(resultChartStream);

            var expectedImagePath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "Resources",
                "chart.png");

            using var expectedChartStream = File.Open(expectedImagePath, FileMode.Open);
            using var expectedChartImage = (Bitmap)Image.FromStream(expectedChartStream);

            var equalImages =
                resultChartImage.Width == expectedChartImage.Width &&
                resultChartImage.Height == expectedChartImage.Height;

            if (equalImages)
            {
                for (int i = 0; i < resultChartImage.Width; i++)
                {
                    for (int j = 0; j < resultChartImage.Height; j++)
                    {
                        // TODO: rewrite this comparison (or use a lib). It have a bad performance.
                        if (resultChartImage.GetPixel(i, j).ToString() !=
                            expectedChartImage.GetPixel(i, j).ToString())
                        {
                            equalImages = false;
                            break;
                        }
                    }
                }
            }

            equalImages.Should().BeTrue();
        }
    }
}

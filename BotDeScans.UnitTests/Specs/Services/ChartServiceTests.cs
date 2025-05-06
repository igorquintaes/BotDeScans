using BotDeScans.App.Models.DTOs;
using BotDeScans.App.Services;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
namespace BotDeScans.UnitTests.Specs.Services;

public class ChartServiceTests : UnitTest
{
    public class CreatePieChart : ChartServiceTests
    {
        [Fact]
        [SuppressMessage("Interoperability", "CA1416", Justification = "Test only runs in Windows OS")]
        [SuppressMessage("CodeQuality", "IDE0079", Justification = "Dumb analysis")]
        public void ShouldCreatePieChartAsExpected()
        {
            var runningInWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Assert.SkipWhen(runningInWindows is false, "Feature only supported in windows.");

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var service = new ChartService();
            var data = new ConsumptionData(500, 100);
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
                        // We can rewrite this comparison (or use a lib). It have a bad performance.
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

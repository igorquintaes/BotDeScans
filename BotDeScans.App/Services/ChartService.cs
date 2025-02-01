using BotDeScans.App.Models;
using ScottPlot;
namespace BotDeScans.App.Services;

public class ChartService
{
    public virtual Stream CreatePieChart(ConsumptionData consumptionData)
    {
        var plt = new Plot();
        var pie = plt.Add.Pie(
        [
            new PieSlice() { Value = consumptionData.FreeSpace, Label = "Livre", LegendText = "Livre", FillColor = Colors.Green },
            new PieSlice() { Value = consumptionData.UsedSpace, Label = "Em uso", LegendText = "Em uso", FillColor = Colors.Red },
        ]);

        pie.ExplodeFraction = .1;
        pie.SliceLabelDistance = 1.4;
        plt.ShowLegend();

        plt.Axes.Frameless();
        plt.HideGrid();

        var imageBytes = plt.GetImageBytes(600, 400, ImageFormat.Png);
        return new MemoryStream(imageBytes);
    }
}

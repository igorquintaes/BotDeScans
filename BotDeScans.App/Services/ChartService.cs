using ScottPlot;
namespace BotDeScans.App.Services;

public class ChartService
{
    public virtual Stream CreatePieChart(
        IDictionary<string, double> labelsValues)
    {
        var plt = new Plot();
        plt.ShowLegend();
        plt.Axes.Frameless();
        plt.HideGrid();

        var pie = plt.Add.Pie(labelsValues
            .Select(x => new PieSlice() { Value = x.Value, Label = x.Key })
            .ToList());

        pie.ExplodeFraction = .1;
        pie.SliceLabelDistance = 1.4;

        var imageBytes = plt.GetImageBytes(600, 400, ImageFormat.Png);
        return new MemoryStream(imageBytes);
    }
}

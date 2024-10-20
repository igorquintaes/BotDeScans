using ScottPlot;
namespace BotDeScans.App.Services;

public class ChartService
{
    public virtual Stream CreatePieChart(
        IDictionary<string, double> labelsValues)
    {
        var values = labelsValues.Select(x => x.Value).ToArray();
        var labels = labelsValues.Select(x => x.Key).ToArray();

        var plt = new Plot(width: 600, height: 400);
        var pie = plt.AddPie(values);
        pie.SliceLabels = labels;
        pie.ShowPercentages = true;
        pie.ShowValues = false;
        pie.ShowLabels = true;
        plt.Legend();

        var imageBytes = plt.GetImageBytes();
        return new MemoryStream(imageBytes);
    }
}

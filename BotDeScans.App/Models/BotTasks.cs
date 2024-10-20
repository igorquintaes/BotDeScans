using BotDeScans.App.Enums;
using BotDeScans.App.Extensions;
using System.Drawing;
namespace BotDeScans.App.Models;

public class BotTasks(IDictionary<StepEnum, StepStatus> data) : Dictionary<StepEnum, StepStatus>(data)
{
    public StepStatus Status =>
        this.All(x => x.Value == StepStatus.Success || x.Value == StepStatus.Skip)
            ? StepStatus.Success
            : this.Any(x => x.Value == StepStatus.Error)
                ? StepStatus.Error
                : StepStatus.Executing;

    public Color ColorStatus
        => Status switch
        {
            StepStatus.Success => Color.Green,
            StepStatus.Error => Color.Red,
            StepStatus.Executing => Color.LightBlue,
            _ => throw new ArgumentOutOfRangeException(nameof(Status), $"Not expected Status value: {Status}")
        };

    public string Header
        => Status switch
        {
            StepStatus.Success => "Executado com sucesso!",
            StepStatus.Error => "Ocorreu um erro na execução!",
            StepStatus.Executing => "Processando...",
            _ => throw new ArgumentOutOfRangeException(nameof(Status), $"Not expected Status value: {Status}")
        };

    public string Details
        => string.Join(
            Environment.NewLine,
            this.Select(task => $"{task.Value.GetEmoji()} - {task.Key.GetDescription()}"));
}

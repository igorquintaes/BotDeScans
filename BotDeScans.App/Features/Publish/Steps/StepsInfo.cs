using BotDeScans.App.Attributes;
using BotDeScans.App.Extensions;
using System.Drawing;
namespace BotDeScans.App.Features.Publish.Steps;

public class StepsInfo(IDictionary<StepEnum, StepStatus> data) : Dictionary<StepEnum, StepStatus>(data)
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
            this.Where(X => X.Value != StepStatus.Skip)
                .Select(task => $"{task.Value.GetEmoji()} - {task.Key.GetDescription()}"));
}

public enum StepStatus
{
    [Emoji("track_next")]
    Skip,
    [Emoji("clock10")]
    Queued,
    [Emoji("fire")]
    Executing,
    [Emoji("white_check_mark")]
    Success,
    [Emoji("warning")]
    Error,
    [Emoji("sos")]
    Fatal
}

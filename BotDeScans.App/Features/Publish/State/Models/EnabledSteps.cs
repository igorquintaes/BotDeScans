using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
namespace BotDeScans.App.Features.Publish.State.Models;

public class EnabledSteps(Dictionary<IStep, StepInfo> steps) : ReadOnlyDictionary<IStep, StepInfo>(steps)
{
    public IEnumerable<(IManagementStep Step, StepInfo Info)> ManagementSteps =>
        this.Where(step => step.Key is IManagementStep)
            .Select(step => ((IManagementStep)step.Key, step.Value));

    public IEnumerable<(IPublishStep Step, StepInfo Info)> PublishSteps =>
        this.Where(step => step.Key is IPublishStep)
            .Select(step => ((IPublishStep)step.Key, step.Value));

    public PublishStatus Status =>
        this.All(x => x.Value.Status == StepStatus.Success)
            ? PublishStatus.Success
            : this.Any(x => x.Value.Status == StepStatus.Error)
                ? PublishStatus.Error
                : PublishStatus.Executing;

    public Color ColorStatus => Status switch
    {
        PublishStatus.Success => Color.Green,
        PublishStatus.Error => Color.Red,
        PublishStatus.Executing => Color.LightBlue,
        _ => throw new ArgumentOutOfRangeException()
    };

    public string StatusMessage => Status.GetDescription();

    public string Details => string.Join(
        Environment.NewLine,
        this.Select(task => $"{task.Value.Status.GetEmoji()} - {task.Key.Name.GetDescription()}"));
}

public enum PublishStatus
{
    [Description("Executado com sucesso!")]
    Success,
    [Description("Ocorreu um erro na execução!")]
    Error,
    [Description("Processando...")]
    Executing
}
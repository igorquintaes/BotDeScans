using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

public class EnabledSteps(Dictionary<IStep, StepInfo> steps) : ReadOnlyDictionary<IStep, StepInfo>(steps)
{
    public EnabledSteps WithUpdatedStepInfo(IStep step, StepInfo updatedInfo)
    {
        var newDict = new Dictionary<IStep, StepInfo>(this) { [step] = updatedInfo };
        return new EnabledSteps(newDict);
    }

    // Merges two EnabledSteps snapshots produced by parallel steps.
    // Overrides an entry from `base` only when it is still queued and `other` has a resolved status,
    // so that a completed status is never overwritten by a pending one from a sibling snapshot.
    public EnabledSteps MergeWith(EnabledSteps? other)
    {
        if (other is null) return this;
        var queued = new[] { StepStatus.QueuedForExecution, StepStatus.QueuedForValidation };
        var merged = new Dictionary<IStep, StepInfo>(this);
        foreach (var (step, updatedInfo) in other)
            if (merged.TryGetValue(step, out var currentInfo)
                && queued.Contains(currentInfo.Status)
                && !queued.Contains(updatedInfo.Status))
                merged[step] = updatedInfo;
        return new EnabledSteps(merged);
    }

    public IEnumerable<(IManagementStep Step, StepInfo Info)> ManagementSteps =>
        this.Where(step => step.Key is IManagementStep and not IConversionStep)
            .Select(step => ((IManagementStep)step.Key, step.Value));

    public IEnumerable<(IConversionStep Step, StepInfo Info)> ConversionSteps =>
        this.Where(step => step.Key is IConversionStep)
            .Select(step => ((IConversionStep)step.Key, step.Value));

    public IEnumerable<(IPublishStep Step, StepInfo Info)> PublishSteps =>
        this.Where(step => step.Key is IPublishStep)
            .Select(step => ((IPublishStep)step.Key, step.Value));

    public Color ColorStatus => Status switch
    {
        PublishStatus.Success => Color.Green,
        PublishStatus.Error => Color.Red,
        _ => Color.LightBlue
    };

    public string MessageStatus => Status.GetDescription();

    public string Details => string.Join(
        Environment.NewLine,
        this.Select(task => $"{task.Value.Status.GetEmoji()} - {task.Key.Name.GetDescription()}"));

    private PublishStatus Status =>
        this.All(x => x.Value.Status is StepStatus.Success or StepStatus.Skip)
            ? PublishStatus.Success
            : this.Any(x => x.Value.Status == StepStatus.Error)
                ? PublishStatus.Error
                : PublishStatus.Executing;

    private enum PublishStatus
    {
        [Description("Executado com sucesso!")]
        Success,
        [Description("Processando...")]
        Executing,
        [Description("Ocorreu um erro na execução!")]
        Error
    }
}
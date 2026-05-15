using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Models.Entities.Enums;
using Microsoft.Extensions.Configuration;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public class StepsService(IConfiguration configuration, IEnumerable<IStep> allSteps)
{
    public const string STEPS_KEY = "Settings:Publish:Steps";

    public virtual EnabledSteps GetEnabledSteps(IReadOnlyCollection<StepName> stepsToSkip)
    {
        var configuredNames = configuration
            .GetValues<StepName>(key: STEPS_KEY)
            .ToHashSet();

        var dependencyNames = allSteps
            .OfType<IPublishStep>()
            .Where(step => configuredNames.Contains(step.Name)
                        && step.Dependency.HasValue)
            .Select(step => step.Dependency.GetValueOrDefault())
            .ToHashSet();

        var enabledSteps = allSteps
            .Where(step => configuredNames.Contains(step.Name)
                     || dependencyNames.Contains(step.Name)
                     || (step is IManagementStep m && m.IsMandatory))
            .OrderBy(step => step.Name)
            .ToList();

        return new EnabledSteps(enabledSteps.ToDictionary(
            step => step,
            step => new StepInfo(step, stepsToSkip.Contains(step.Name))));
    }
}

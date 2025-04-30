using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.State.Models;
using BotDeScans.App.Features.Publish.Steps;
using BotDeScans.App.Features.Publish.Steps.Enums;
using Microsoft.Extensions.Configuration;
namespace BotDeScans.App.Features.Publish;

public class PublishService(IConfiguration configuration, IEnumerable<IStep> allSteps)
{
    public const string STEPS_KEY = "Settings:Publish:Steps";

    public virtual EnabledSteps GetEnabledSteps()
    {
        var configurationSteps = GetConfigurationSteps(configuration, allSteps);
        var dependencySteps = GetDependencySteps(configurationSteps, allSteps);
        var mandatorySteps = GetMandatorySteps(allSteps);

        var enabledSteps = configurationSteps
            .Union(dependencySteps)
            .Union(mandatorySteps)
            .DistinctBy(step => step.Name)
            .OrderBy(step => step.Name)
            .ToList();

        return new EnabledSteps(enabledSteps.ToDictionary(
            step => step,
            step => new StepInfo(step)));
    }

    /// <summary>
    /// Steps from configuration File
    /// </summary>
    private static List<IStep> GetConfigurationSteps(IConfiguration configuration, IEnumerable<IStep> allSteps)
    {
        var stepNames = configuration.GetRequiredValues<StepName>(
            key: STEPS_KEY,
            value => Enum.Parse(typeof(StepName), value));

        return allSteps
            .Where(step => stepNames.Contains(step.Name))
            .ToList();
    }

    /// <summary>
    /// Steps that needs to be included due configuration's steps
    /// </summary>
    private static List<IStep> GetDependencySteps(IEnumerable<IStep> configurationSteps, IEnumerable<IStep> allSteps)
    {
        var dependencyNames = configurationSteps
            .OfType<IPublishStep>()
            .Where(p => p.Dependency is not null)
            .Select(p => p.Dependency)
            .ToList();

        return allSteps
            .Where(step => dependencyNames.Contains(step.Name))
            .ToList();
    }

    /// <summary>
    /// Steps that needs to be included
    /// </summary>
    private static List<IStep> GetMandatorySteps(IEnumerable<IStep> allSteps) => allSteps
        .OfType<IManagementStep>()
        .Where(step => step.IsMandatory)
        .Select(step => (IStep)step)
        .ToList();
}

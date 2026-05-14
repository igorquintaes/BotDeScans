using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public interface IStep
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken);
    StepName Name { get; }
    StepType Type { get; }
    bool ContinueOnError => false;
}

public interface IManagementStep : IStep
{
    bool IsMandatory { get; }
}

public interface IPublishStep : IStep
{
    StepName? Dependency { get; }
    abstract Task<Result> ValidateAsync(CancellationToken cancellationToken);
}
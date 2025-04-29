using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public interface IStep
{
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken);
    public StepName Name { get; }
    public StepType Type { get; }
}

public interface IManagementStep : IStep
{
    public bool IsMandatory { get; }
}

public interface IPublishStep : IStep
{
    public StepName? Dependency { get; }
    public abstract Task<Result> ValidateAsync(CancellationToken cancellationToken);
}
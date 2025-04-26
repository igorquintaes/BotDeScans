using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public interface IStep
{
    public StepType Type { get; }
    public StepName Name { get; }
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken);
}

public abstract class ManagementStep : IStep
{
    public StepType Type => StepType.Management;
    public abstract StepName Name { get; }
    public abstract Task<Result> ExecuteAsync(CancellationToken cancellationToken);
}

public abstract class PublishStep : IStep
{
    public abstract StepType Type { get; }
    public abstract StepName Name { get; }
    public abstract Task<Result> ValidateAsync(CancellationToken cancellationToken);
    public abstract Task<Result> ExecuteAsync(CancellationToken cancellationToken);
}
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Steps;

public interface IStep
{
    Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken);
    StepName Name { get; }
    StepType Type { get; }
    bool ContinueOnError => false;
}

public interface IManagementStep : IStep
{
    bool IsMandatory { get; }
}

// Marks a management step that transforms already-downloaded files (e.g. zip, pdf, epub).
// These steps read from State.OriginContentFolder and write to an isolated output directory,
// so multiple conversion steps can run in parallel. A shared I/O throttle in the Handler
// caps the number of concurrent conversions to avoid file-descriptor exhaustion.
public interface IConversionStep : IManagementStep { }

public interface IPublishStep : IStep
{
    StepName? Dependency { get; }
    abstract Task<Result> ValidateAsync(State state, CancellationToken cancellationToken);
}
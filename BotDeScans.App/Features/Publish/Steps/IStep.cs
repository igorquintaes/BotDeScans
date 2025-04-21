using BotDeScans.App.Features.Publish.Steps.Enums;
using FluentResults;
namespace BotDeScans.App.Features.Publish.Steps;

public interface IStep
{
    public Task<Result> ExecuteAsync(CancellationToken cancellationToken);
    public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken);
    public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken cancellationToken);
    public StepName StepName { get; }
    public StepType StepType => StepsInfo.StepNameType[StepName];
}
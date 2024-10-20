using BotDeScans.App.Enums;
using FluentResults;

namespace BotDeScans.App.Services.Publish.Steps
{
    public interface IStep
    {
        public Task<Result> ExecuteAsync(CancellationToken cancellationToken);
        public Task<Result> ValidateBeforeFilesManagementAsync(CancellationToken cancellationToken);
        public Task<Result> ValidateAfterFilesManagementAsync(CancellationToken cancellationToken);
        public StepEnum StepName { get; }
        public StepType StepType { get; }
    }
}

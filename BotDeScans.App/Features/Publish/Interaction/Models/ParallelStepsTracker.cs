using BotDeScans.App.Features.Publish.Interaction.Steps;
using FluentResults;

namespace BotDeScans.App.Features.Publish.Interaction.Models;

/// <summary>
/// Coordinates state updates and Discord tracking during a parallel step phase.
/// Each step executes freely (no lock), then calls <see cref="ApplyAndNotifyAsync"/> which
/// acquires a lock, merges the step's result into the shared current state, and sends
/// a single Discord update — so users see real-time progress without status regressions.
/// </summary>
public class ParallelStepsTracker(
    State initialState,
    Func<State, CancellationToken, Task<Result<State>>> sendTrackingUpdate)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private State _currentState = initialState;
    private Result _aggregateTrackingResult = Result.Ok();

    public State CurrentState => _currentState;
    public Result AggregateTrackingResult => _aggregateTrackingResult;

    /// <summary>
    /// Called by each parallel step after it finishes executing.
    /// Merges the step's updated <see cref="StepInfo"/> into the shared state (using the latest
    /// version, not the snapshot the step started from) and sends the tracking update while holding
    /// the lock — so the embed always reflects the union of all completed steps so far.
    /// </summary>
    public async Task<Result> ApplyAndNotifyAsync(
        Result stepResult,
        IStep step,
        State stepSnapshot,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Merge file paths / links from this step's output into the latest shared state.
            var merged = stepSnapshot.Steps is not null
                ? Handler.MergeStates(_currentState, stepSnapshot)
                : _currentState;

            // Apply this step's status onto the freshly merged state.
            var updatedInfo = merged.Steps[step].UpdateStatus(stepResult);
            var stateToSend = merged with { Steps = merged.Steps.WithUpdatedStepInfo(step, updatedInfo) };

            var trackingResult = await sendTrackingUpdate(stateToSend, cancellationToken);
            _aggregateTrackingResult = Result.Merge(_aggregateTrackingResult, trackingResult.ToResult());

            _currentState = trackingResult.IsSuccess ? trackingResult.Value : stateToSend;

            return _aggregateTrackingResult;
        }
        finally
        {
            _lock.Release();
        }
    }
}

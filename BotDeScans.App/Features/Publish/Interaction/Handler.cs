using BotDeScans.App.Extensions;
using BotDeScans.App.Features.Publish.Interaction.Models;
using BotDeScans.App.Features.Publish.Interaction.Steps;
using BotDeScans.App.Features.Publish.Interaction.Steps.Enums;
using BotDeScans.App.Models.Entities.Enums;
using FluentResults;
using Serilog;
using System.Diagnostics;

namespace BotDeScans.App.Features.Publish.Interaction;

public class Handler(
    DiscordPublisher discordPublisher)
{
    public virtual async Task<Result<State>> ExecuteAsync(State state, CancellationToken cancellationToken)
    {
        var managementSteps = state.Steps.ManagementSteps.ToList();
        var conversionSteps = state.Steps.ConversionSteps.ToList();
        var publishSteps = state.Steps.PublishSteps.ToList();

        var trackingResult = await discordPublisher.UpdateTrackingMessageAsync(state, cancellationToken);
        if (trackingResult.IsFailed)
            return trackingResult;

        var currentState = trackingResult.Value;
        var currentResult = trackingResult.ToResult();

        // Phase 1: Setup, Download, Compress — strictly sequential.
        var managementExecution = await RunSequentialChainAsync(
            currentResult,
            currentState,
            managementSteps.Select(data => ((IStep)data.Step, data.Info)),
            cancellationToken);
        if (managementExecution.ShouldStop)
            return managementExecution.Result.IsFailed
                ? managementExecution.Result.ToResult<State>()
                : Result.Ok(managementExecution.State);

        currentState = managementExecution.State;
        currentResult = managementExecution.Result;

        // Phase 2: Conversion steps (ZipFiles, PdfFiles, …) — run in parallel.
        if (conversionSteps.Count > 0)
        {
            var conversionExecution = await RunParallelConversionAsync(
                currentResult,
                currentState,
                conversionSteps,
                cancellationToken);
            if (conversionExecution.ShouldStop)
                return conversionExecution.Result.IsFailed
                    ? conversionExecution.Result.ToResult<State>()
                    : Result.Ok(conversionExecution.State);

            currentState = conversionExecution.State;
            currentResult = conversionExecution.Result;
        }

        // Phase 3: Validate all publish steps sequentially.
        var validationExecution = await RunSequentialChainAsync(
            currentResult,
            currentState,
            publishSteps.Select(data => ((IStep)data.Step, data.Info)),
            RunValidationAsync,
            cancellationToken);
        if (validationExecution.ShouldStop)
            return validationExecution.Result.IsFailed
                ? validationExecution.Result.ToResult<State>()
                : Result.Ok(validationExecution.State);

        currentState = validationExecution.State;

        // Phase 4: Publish steps — grouped by Dependency, each group in parallel.
        var publishExecution = await RunParallelDagAsync(
            validationExecution.Result,
            currentState,
            publishSteps,
            cancellationToken);

        return publishExecution.Result.IsFailed
            ? publishExecution.Result.ToResult<State>()
            : Result.Ok(publishExecution.State);
    }

    // Runs steps sequentially, used for management and validation phases.
    private async Task<(Result Result, State State, bool ShouldStop)> RunSequentialChainAsync(
        Result aggregate,
        State state,
        IEnumerable<(IStep Step, StepInfo Info)> chain,
        Func<(IStep Step, StepInfo Info), State, CancellationToken, Task<Result<State>>> stepAction,
        CancellationToken cancellationToken)
    {
        foreach (var item in chain)
        {
            var stepResult = await stepAction(item, state, cancellationToken);
            if (stepResult.IsSuccess)
                state = stepResult.Value;

            aggregate = Result.Merge(aggregate, stepResult.ToResult());

            if (stepResult.IsFailed && item.Step.ContinueOnError is false)
                return (aggregate, state, true);
        }

        return (aggregate, state, false);
    }

    // Overload that defaults to RunStepAsync, used for management execution.
    private Task<(Result Result, State State, bool ShouldStop)> RunSequentialChainAsync(
        Result aggregate,
        State state,
        IEnumerable<(IStep Step, StepInfo Info)> chain,
        CancellationToken cancellationToken)
        => RunSequentialChainAsync(aggregate, state, chain, RunStepAsync, cancellationToken);

    // Runs conversion steps in parallel using Task.WhenAll.
    // Each step reads from State.OriginContentFolder (read-only, safe for concurrent access on all
    // major OS/filesystems) and writes to its own isolated directory created by CreateScopedDirectory,
    // so there are no write conflicts. CPU/I/O throttling is the responsibility of each individual
    // step implementation (e.g. CompressFilesStep already uses Parallel.ForEachAsync internally).
    private async Task<(Result Result, State State, bool ShouldStop)> RunParallelConversionAsync(
        Result aggregate,
        State state,
        IEnumerable<(IConversionStep Step, StepInfo Info)> conversionSteps,
        CancellationToken cancellationToken)
    {
        var items = conversionSteps.ToList();
        var results = await Task.WhenAll(
            items.Select(item => RunStepAsync((item.Step, item.Info), state, cancellationToken)));

        foreach (var (stepResult, item) in results.Zip(items))
        {
            if (stepResult.IsSuccess)
                state = MergeStates(state, stepResult.Value);

            aggregate = Result.Merge(aggregate, stepResult.ToResult());
        }

        var hasFatalFailure = results
            .Zip(items, (r, item) => (Result: r, Step: (IStep)item.Step))
            .Any(x => x.Result.IsFailed && !x.Step.ContinueOnError);

        return (aggregate, state, hasFatalFailure);
    }

    // Runs publish steps grouped by Dependency, each group in parallel, groups sequentially.
    private async Task<(Result Result, State State)> RunParallelDagAsync(
        Result aggregate,
        State state,
        IEnumerable<(IPublishStep Step, StepInfo Info)> publishSteps,
        CancellationToken cancellationToken)
    {
        // Group by dependency: null-dependency steps run after all dependency-based groups
        var groups = publishSteps
            .GroupBy(x => x.Step.Dependency)
            .OrderBy(g => g.Key.HasValue ? 0 : 1)  // dependency groups first, null last
            .ToList();

        foreach (var group in groups)
        {
            var groupItems = group.ToList();

            // Run all steps in this group in parallel
            var tasks = groupItems.Select(item =>
                RunStepAsync((item.Step, item.Info), state, cancellationToken));

            var results = await Task.WhenAll(tasks);

            // Merge states from parallel steps (each writes to disjoint properties)
            foreach (var stepResult in results)
            {
                if (stepResult.IsSuccess)
                    state = MergeStates(state, stepResult.Value);

                aggregate = Result.Merge(aggregate, stepResult.ToResult());
            }

            // Stop the entire DAG if any non-continuable step failed
            var hasFatalFailure = results
                .Zip(groupItems, (r, item) => (Result: r, Step: item.Step))
                .Any(x => x.Result.IsFailed && !x.Step.ContinueOnError);

            if (hasFatalFailure)
                return (aggregate, state);
        }

        return (aggregate, state);
    }

    // Merges two State instances produced by parallel steps, combining their non-null properties.
    // Steps are merged entry-by-entry so status updates from all parallel steps are preserved.
    public static State MergeStates(State @base, State updated) =>
        @base with
        {
            Steps = @base.Steps?.MergeWith(updated.Steps) ?? updated.Steps,
            TrackingMessage = updated.TrackingMessage,
            ZipFilePath = updated.ZipFilePath ?? @base.ZipFilePath,
            PdfFilePath = updated.PdfFilePath ?? @base.PdfFilePath,
            BoxPdfReaderKey = updated.BoxPdfReaderKey ?? @base.BoxPdfReaderKey,
            MegaZipLink = updated.MegaZipLink ?? @base.MegaZipLink,
            MegaPdfLink = updated.MegaPdfLink ?? @base.MegaPdfLink,
            DriveZipLink = updated.DriveZipLink ?? @base.DriveZipLink,
            DrivePdfLink = updated.DrivePdfLink ?? @base.DrivePdfLink,
            BoxZipLink = updated.BoxZipLink ?? @base.BoxZipLink,
            BoxPdfLink = updated.BoxPdfLink ?? @base.BoxPdfLink,
            MangaDexLink = updated.MangaDexLink ?? @base.MangaDexLink,
            SakuraMangasLink = updated.SakuraMangasLink ?? @base.SakuraMangasLink,
            BloggerLink = updated.BloggerLink ?? @base.BloggerLink,
            BloggerImageAsBase64 = updated.BloggerImageAsBase64 ?? @base.BloggerImageAsBase64,
        };

    private async Task<Result<State>> RunValidationAsync(
        (IStep Step, StepInfo Info) data,
        State state,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok(state);

        var result = await ((IPublishStep)data.Step).SafeCallAsync(x => x.ValidateAsync(state, cancellationToken));
        return await HandleResult(result, data.Step, state, cancellationToken);
    }

    private async Task<Result<State>> RunStepAsync(
        (IStep Step, StepInfo Info) data,
        State state,
        CancellationToken cancellationToken)
    {
        if (data.Info.Status == StepStatus.Skip)
            return Result.Ok(state);

        var stopwatch = Stopwatch.StartNew();
        var result = await data.Step.SafeCallAsync(x => x.ExecuteAsync(state, cancellationToken));
        stopwatch.Stop();

        Log.Information(
            "Publish step '{StepName}' ExecuteAsync finished in {ElapsedMilliseconds} ms with status {Status}.",
            data.Step.Name,
            stopwatch.ElapsedMilliseconds,
            result.IsSuccess ? "Success" : "Failure");

        var handleResult = await HandleResult(result.ToResult(), data.Step, state, cancellationToken);
        return handleResult.IsFailed
            ? handleResult
            : Result.Ok(result.IsSuccess ? result.Value with { Steps = handleResult.Value.Steps } : handleResult.Value);
    }

    private async Task<Result<State>> HandleResult(
        Result result,
        IStep step,
        State state,
        CancellationToken cancellationToken)
    {
        var updatedInfo = state.Steps[step].UpdateStatus(result);
        var updatedSteps = state.Steps.WithUpdatedStepInfo(step, updatedInfo);
        var updatedState = state with { Steps = updatedSteps };

        var feedbackResult = await discordPublisher.SynchronizedUpdateTrackingMessageAsync(updatedState, cancellationToken);
        if (feedbackResult.IsFailed)
            return Result.Merge(result, feedbackResult.ToResult()).ToResult<State>();

        var merged = Result.Merge(result, feedbackResult.ToResult());
        return merged.IsFailed
            ? merged.ToResult<State>()
            : feedbackResult;
    }
}

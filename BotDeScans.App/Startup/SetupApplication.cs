using BotDeScans.App.Extensions;
using BotDeScans.App.Startup.Setup;
using FluentResults;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BotDeScans.App.Startup;

public class SetupApplication(
    IEnumerable<ISetup> setups, 
    IHostApplicationLifetime appLifetime) 
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var aggregateResult = new Result();

        await Parallel.ForEachAsync(setups, cancellationToken, async (setup, ct) =>
        {
            try
            {
                var setupResult = await setup.ExecuteAsync(ct);
                aggregateResult.WithReasons(setupResult.Reasons);
            }
            catch (Exception ex)
            {
                var setupName = setup.GetType().Name;

                Console.WriteLine($"{setupName} EXCEPTIONAL ERROR!");

                var error = new ExceptionalError($"An exceptional error occurred during {setupName} execution.", ex);
                aggregateResult.WithError(error);
            }
        });

        LogSetup(aggregateResult);

        if (aggregateResult.IsFailed)
            appLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void LogSetup(Result result)
    {
        LogSuccesses(result);
        LogErrors(result);
    }

    private static void LogSuccesses(Result result)
    {
        var successesInfo = result.Successes.GetSuccessesInfo().Select(x => x.ToString());
        var SuccessesText = string.Join(Environment.NewLine, successesInfo);
        Log.Information(SuccessesText);

        var defaultConsoleForegroundColor = Console.ForegroundColor;
        var defaultBackgroundForegroundColor = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(SuccessesText);

        Console.ForegroundColor = defaultConsoleForegroundColor;
        Console.WriteLine();
    }

    private static void LogErrors(Result result)
    {
        if (result.IsSuccess)
            return;

        var errorsInfo = result.Errors.GetErrorsInfo().Select(x => x.ToString());
        var errorsText = string.Join(Environment.NewLine, errorsInfo);
        Log.Error(errorsText);

        var defaultConsoleForegroundColor = Console.ForegroundColor;
        var defaultBackgroundForegroundColor = Console.BackgroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.BackgroundColor = ConsoleColor.Red;

        Console.WriteLine(errorsText);

        Console.ForegroundColor = defaultConsoleForegroundColor;
        Console.BackgroundColor = defaultBackgroundForegroundColor;
        Console.ReadLine();
    }
}

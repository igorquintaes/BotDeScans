using BotDeScans.App.Extensions;
using BotDeScans.App.Services.Initializations.Setups;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Collections.Concurrent;

namespace BotDeScans.App.Services.Initializations;

public class SetupApplication(
    IEnumerable<ISetup> setups,
    IServiceProvider serviceProvider,
    IHostApplicationLifetime appLifetime)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var results = new ConcurrentBag<Result>();
        await Parallel.ForEachAsync(setups, cancellationToken, async (setup, ct) =>
        {
            try
            {
                var setupResult = await setup.ExecuteAsync(ct);
                results.Add(setupResult);

                // A inicialização do Google Drive depende da execução do setup de ClientsSetup,
                // responsável por criar o client do Google Drive. De tal forma, o setup do Google Drive
                // só pode ser executado se o setup de ClientsSetup for bem-sucedido.
                if (setup is ClientsSetup && setupResult.IsSuccess)
                {
                    var setupDriverResult = await serviceProvider
                        .GetRequiredService<GoogleDriveInitializer>()
                        .ExecuteAsync(ct);

                    results.Add(setupDriverResult);
                }
            }
            catch (Exception ex)
            {
                var setupName = setup.GetType().Name;

                Console.WriteLine($"{setupName} EXCEPTIONAL ERROR!");

                var error = new ExceptionalError($"An exceptional error occurred during {setupName} execution.", ex);
                results.Add(Result.Fail(error));
            }
        });

        var aggregateResult = Result.Merge([.. results]);

        LogSetup(aggregateResult);

        if (aggregateResult.IsFailed)
            appLifetime.StopApplication();

        LogSuccesses("Application is running.");
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
        var successesText = string.Join(Environment.NewLine, successesInfo);
        LogSuccesses(successesText);
    }

    private static void LogSuccesses(string successesText)
    {
        Log.Information(successesText);

        var defaultConsoleForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(successesText);

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
    }
}

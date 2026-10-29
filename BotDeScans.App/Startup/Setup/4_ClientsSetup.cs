using BotDeScans.App.Services.Initializations;
using FluentResults;

namespace BotDeScans.App.Startup.Setup;

public class ClientsSetup(SetupClientsService setupClientsService) : ISetup
{
    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        var result = await setupClientsService.SetupAsync(cancellationToken);
        if (result.IsFailed)
            return result;

        return result.WithSuccess("External Services setup warmup completed successfully");
    }
}

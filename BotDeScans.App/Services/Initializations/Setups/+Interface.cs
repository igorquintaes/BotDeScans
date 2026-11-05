using FluentResults;

namespace BotDeScans.App.Services.Initializations.Setups;

public interface ISetup
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken);
}

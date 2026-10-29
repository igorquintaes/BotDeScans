using FluentResults;

namespace BotDeScans.App.Startup.Setup;

public interface ISetup
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken);
}

using FluentResults;

namespace BotDeScans.App.Services.Initializations.Factories.Base;

public interface IClientFactory
{
    bool Enabled { get; }
    Task<Result<object>> SafeCreateObjectAsync(CancellationToken cancellationToken);
    Task<Result> HealthCheckAsync(object client, CancellationToken cancellationToken);
}

public abstract class ClientFactory<TClient> : IClientFactory
{
    public abstract bool Enabled { get; }
    public abstract Task<Result<TClient>> CreateAsync(CancellationToken cancellationToken);
    public abstract Task<Result> HealthCheckAsync(TClient client, CancellationToken cancellationToken);

    public async Task<Result<TClient>> SafeCreateAsync(CancellationToken cancellationToken)
    {
        var errorMessage = $"Failed to create a client of type {typeof(TClient).Name}.";

        return await Result.Try(
            action: () => CreateAsync(cancellationToken),
            catchHandler: ex => new Error(errorMessage).CausedBy(ex));
    }

    public static Result<string> ConfigFileExists(string fileName)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", fileName);
        var errorMessage = $"Unable to find {typeof(TClient).Name} file: {filePath}";
        var errors = File.Exists(filePath) ? [] : new[] { errorMessage };

        return Result.Ok(filePath).WithErrors(errors);
    }

    protected static Result<FileStream> GetConfigFileAsStream(string fileName)
    {
        var configFileResult = ConfigFileExists(fileName);

        return configFileResult.IsSuccess
            ? new FileStream(configFileResult.Value, FileMode.Open, FileAccess.Read)
            : configFileResult.ToResult();
    }

    async Task<Result<object>> IClientFactory.SafeCreateObjectAsync(CancellationToken cancellationToken)
    {
        var result = await SafeCreateAsync(cancellationToken);
        return result.IsSuccess
            ? Result.Ok<object>(result.Value!)
            : result.ToResult<object>();
    }

    Task<Result> IClientFactory.HealthCheckAsync(object client, CancellationToken cancellationToken)
        => HealthCheckAsync((TClient)client, cancellationToken);
}

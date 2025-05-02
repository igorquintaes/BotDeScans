using FluentResults;
namespace BotDeScans.App.Services.Initializations.Factories.Base;

public abstract class ClientFactory<TClient>
{
    public abstract bool Enabled { get; }
    public abstract Task<Result<TClient>> CreateAsync(CancellationToken cancellationToken);
    public abstract Task<Result> HealthCheckAsync(TClient client, CancellationToken cancellationToken);

    public async Task<Result<TClient>> SafeCreateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await CreateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return new Error($"Failed to create a client of type {typeof(TClient).Name}.").CausedBy(ex);
        }
    }

    public static Result ConfigFileExists(string fileName)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", fileName);
        if (File.Exists(filePath))
            return Result.Ok();

        return Result.Fail($"Unable to find {typeof(TClient).Name} file: {filePath}");
    }

    protected static Result<FileStream> GetConfigFileAsStream(string fileName)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", fileName);
        if (File.Exists(filePath))
            return Result.Ok(new FileStream(filePath, FileMode.Open, FileAccess.Read));

        return Result.Fail($"Unable to find {typeof(TClient).Name} file: {filePath}");
    }
}

using FluentResults;
namespace BotDeScans.App.Services.ExternalClients;

// todo: mangadex initialization as client (em outra classe, tipo um Client mesmo)

public abstract class ExternalClientBase
{
    public abstract Task<Result> InitializeAsync(CancellationToken cancellationToken = default);
}

public abstract class ExternalClientBase<TClient> : ExternalClientBase
{
    public TClient Client { get; set; } = default!;
    protected abstract bool Enabled { get; }

    protected static Result<FileStream> GetCredentialsAsStream(string fileName)
    {
        FileStream? fs = null;
        try
        {
            fs = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", fileName), FileMode.Open, FileAccess.Read);
            return Result.Ok(fs);
        }
        catch
        {
            fs?.Dispose();
            return Result.Fail($"Unable to find {typeof(TClient).Name} credentials: {fileName}");
        }
    }
}

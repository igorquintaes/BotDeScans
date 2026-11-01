using BotDeScans.App.Infra;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace BotDeScans.App.Services.Initializations.Setups;

public class DatabaseSetup(DatabaseContext databaseContext) : ISetup
{
    public virtual async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Database Warming Up...");

        if (File.Exists(DatabaseContext.DbPath) is false)
            await File.WriteAllBytesAsync(DatabaseContext.DbPath, [], cancellationToken);

        await databaseContext.Database.MigrateAsync(cancellationToken);

        return Result.Ok().WithSuccess("Database setup completed successfully.");
    }
}
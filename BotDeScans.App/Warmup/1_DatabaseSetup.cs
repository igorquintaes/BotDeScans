using BotDeScans.App.Infra;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BotDeScans.App.Warmup;

public static class DatabaseSetup
{
    public static async Task<Result> RunAsync(AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("Database Warming Up...");

            if (File.Exists(DatabaseContext.DbPath) is false)
                File.WriteAllBytes(DatabaseContext.DbPath, []);

            await scope.ServiceProvider.GetRequiredService<DatabaseContext>()
                       .Database.MigrateAsync(cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database setup warmup EXCEPTIONAL ERROR!");
            var error = new ExceptionalError("An exceptional error occurred during Database setup.", ex);

            return Result.Fail(error);
        }
    }
}

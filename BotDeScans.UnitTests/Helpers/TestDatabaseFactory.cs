using BotDeScans.App.Infra;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BotDeScans.UnitTests.Helpers;

public class TestDatabaseFactory : IDisposable
{
    private DatabaseContext? context;
    private DbConnection? connection;

    private DbContextOptions<DbContext> Options =>
        new DbContextOptionsBuilder<DbContext>()
            .UseSqlite(connection!)
            .Options;

    public DatabaseContext CreateContext()
    {
        if (connection == null)
        {
            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            context = new DatabaseContext(Options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        return context!;
    }

    public void Dispose()
    {
        context?.Dispose();
        connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}
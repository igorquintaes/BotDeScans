using BotDeScans.App.Models;
using Microsoft.EntityFrameworkCore;
namespace BotDeScans.App.Infra;

public class DatabaseContext : DbContext
{
    public DbSet<Title> Titles { get; protected init; } = default!;
    public DbSet<TitleReference> TitleReferences { get; protected init; } = default!;

    public static string DbPath { get; }

    static DatabaseContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "config", "database.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}

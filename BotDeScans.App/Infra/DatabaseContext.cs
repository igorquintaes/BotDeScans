using BotDeScans.App.Models;
using Microsoft.EntityFrameworkCore;
namespace BotDeScans.App.Infra;

public class DatabaseContext : DbContext
{
    public virtual DbSet<Title> Titles { get; protected init; } = default!;
    public virtual DbSet<TitleReference> TitleReferences { get; protected init; } = default!;

    public static string DbPath { get; } = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "database.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Title>()
            .HasMany(e => e.References)
            .WithOne(e => e.Title)
            .HasForeignKey(e => e.TitleId)
            .IsRequired();

        modelBuilder.Entity<TitleReference>()
            .HasIndex(e => new { e.TitleId, e.Key })
            .IsUnique();

        modelBuilder.Entity<Title>()
            .HasIndex(e => e.Name)
            .IsUnique();
    }
}

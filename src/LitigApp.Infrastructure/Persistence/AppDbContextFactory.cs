using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LitigApp.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` design-time tooling. Without it, EF Core runs the app's real
/// Program.cs Main instead, which needs the full host config — unavailable during the
/// Docker build stage. Resolves the connection string from appsettings.Development.json,
/// then ConnectionStrings__Postgres from the environment (what Railway's Pre-Deploy
/// Command sees at runtime), then a throwaway value for the Docker build stage, where
/// `migrations bundle` only inspects the model and never opens a connection.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // .invalid is the RFC 2606 reserved TLD — never resolves, can't pass as real.
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=design-time-only.invalid;Database=unused;Username=unused;Password=unused";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}

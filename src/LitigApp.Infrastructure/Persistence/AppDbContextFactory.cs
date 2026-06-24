using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LitigApp.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` design-time tooling (migrations add/bundle, and the bundle's
/// own `database update` when it runs). Without this, EF Core falls back to running the
/// app's real Program.cs Main to discover the DbContext, which requires the full host
/// (Jwt options, etc.) to build successfully — unavailable during the Docker build stage,
/// where Railway hasn't injected any runtime env vars yet.
///
/// Resolves the connection string the same places the app would: local
/// appsettings.Development.json (so `dotnet ef database update` keeps working exactly as
/// documented, with no extra flags), then ConnectionStrings__Postgres from the environment
/// (what Railway injects when the Pre-Deploy Command actually runs the bundle in
/// production). Falls back to a throwaway value only when neither is available, which is
/// the Docker build stage — `migrations bundle` there only inspects the model and never
/// opens a connection, so the value doesn't matter.
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

        // design-time-only.invalid uses the RFC 2606 reserved .invalid TLD — guaranteed to
        // never resolve, so it can't be mistaken for a real (if wrong) host.
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=design-time-only.invalid;Database=unused;Username=unused;Password=unused";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}

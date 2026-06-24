using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LitigApp.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` design-time tooling (migrations add/bundle). Without this,
/// EF Core falls back to running the app's real Program.cs Main to discover the
/// DbContext, which requires ConnectionStrings:Postgres to be configured — unavailable
/// during the Docker build stage where Railway hasn't injected runtime env vars yet.
/// The connection string here is never opened; migrations bundle only inspects the model.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Database=litigapp_designtime;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}

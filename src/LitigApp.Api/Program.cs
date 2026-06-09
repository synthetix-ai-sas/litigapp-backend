using LitigApp.Infrastructure;
using LitigApp.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// CLI: `dotnet run --project src/LitigApp.Api -- seed-catalog`
// Seeds the geographic/judicial catalog and exits without starting the web host.
if (args.Contains("seed-catalog"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await CatalogSeeder.SeedAsync(db);
    return;
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }

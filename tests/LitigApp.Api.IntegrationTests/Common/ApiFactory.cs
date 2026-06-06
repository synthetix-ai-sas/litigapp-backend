using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LitigApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace LitigApp.Api.IntegrationTests.Common;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Must match Jwt:Secret in appsettings.json so the app can validate tokens without extra config.
    public const string TestJwtSecret = "dev-placeholder-override-via-Jwt__Secret-env-var-in-prod-min32chars";
    public const string TestJwtIssuer = "LitigApp";
    public const string TestJwtAudience = "LitigApp";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
            });
        });

        // Override JWT signing key after all options are applied, to ensure test secret wins.
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
                    options.TokenValidationParameters.ValidIssuer = TestJwtIssuer;
                    options.TokenValidationParameters.ValidAudience = TestJwtAudience;
                });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Environment variables override appsettings.json — reliable in all .NET hosting models
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SeedTestDataAsync(db);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public string GenerateTestToken(string userId = "test-user-id")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, "test@litigapp.co"),
        };
        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task SeedTestDataAsync(AppDbContext db)
    {
        if (await db.Departments.AnyAsync()) return;

        db.Departments.AddRange(
            new Domain.Catalog.Department { Id = "17", Name = "Caldas" },
            new Domain.Catalog.Department { Id = "63", Name = "Quindío" },
            new Domain.Catalog.Department { Id = "66", Name = "Risaralda" }
        );
        db.Cities.AddRange(
            new Domain.Catalog.City { Id = "17001", DepartmentId = "17", Name = "Manizales" },
            new Domain.Catalog.City { Id = "17380", DepartmentId = "17", Name = "La Dorada" },
            new Domain.Catalog.City { Id = "63001", DepartmentId = "63", Name = "Armenia" },
            new Domain.Catalog.City { Id = "66001", DepartmentId = "66", Name = "Pereira" },
            new Domain.Catalog.City { Id = "66045", DepartmentId = "66", Name = "Apía" }
        );
        db.Specialties.AddRange(
            new Domain.Catalog.Specialty { Code = "03", Name = "CIVIL" },
            new Domain.Catalog.Specialty { Code = "05", Name = "LABORAL" }
        );
        db.Entities.AddRange(
            new Domain.Catalog.Entity { Code = "01", Name = "JUZGADO" },
            new Domain.Catalog.Entity { Code = "71", Name = "CENTRO DE SERVICIOS JUDICIALES" }
        );

        var courtId1 = Guid.NewGuid();
        var courtId2 = Guid.NewGuid();
        var courtId3 = Guid.NewGuid();
        db.Courts.AddRange(
            new Domain.Catalog.Court { Id = courtId1, OfficialCode = "170014003010", CityId = "17001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 1, Name = "JUZGADO 001 CIVIL MUNICIPAL DE MANIZALES", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = courtId2, OfficialCode = "170014003020", CityId = "17001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 2, Name = "JUZGADO 002 CIVIL MUNICIPAL DE MANIZALES", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = courtId3, OfficialCode = "170014005010", CityId = "17001", EntityCode = "01", SpecialtyCode = "05", CourtNumber = 1, Name = "JUZGADO 001 LABORAL DEL CIRCUITO DE MANIZALES", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "630014003010", CityId = "63001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 1, Name = "JUZGADO 001 CIVIL MUNICIPAL DE ARMENIA", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "660014003010", CityId = "66001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 1, Name = "JUZGADO 001 CIVIL MUNICIPAL DE PEREIRA", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "660014003020", CityId = "66001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 2, Name = "JUZGADO 002 CIVIL MUNICIPAL DE PEREIRA", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "660014003030", CityId = "66001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 3, Name = "JUZGADO 003 CIVIL MUNICIPAL DE PEREIRA", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "660014005010", CityId = "66001", EntityCode = "01", SpecialtyCode = "05", CourtNumber = 1, Name = "JUZGADO 001 LABORAL DEL CIRCUITO DE PEREIRA", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "660014003040", CityId = "66001", EntityCode = "01", SpecialtyCode = "03", CourtNumber = 4, Name = "JUZGADO 004 CIVIL MUNICIPAL DE PEREIRA", IsActive = false, CreatedAt = DateTimeOffset.UtcNow },
            new Domain.Catalog.Court { Id = Guid.NewGuid(), OfficialCode = "630014005010", CityId = "63001", EntityCode = "01", SpecialtyCode = "05", CourtNumber = 1, Name = "JUZGADO 001 LABORAL DEL CIRCUITO DE ARMENIA", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        );

        await db.SaveChangesAsync();
    }
}

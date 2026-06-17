using System.Net.Http.Headers;
using LitigApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace LitigApp.Api.IntegrationTests.Common;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace JWT with a test auth handler that accepts any Bearer token.
            // This avoids JWT key/clock issues in CI and keeps tests focused on behavior.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
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

    /// <summary>Returns a bearer token accepted by <see cref="TestAuthHandler"/>.</summary>
    public static string GenerateTestToken() => TestAuthHandler.TestBearerToken;

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

        SeedProcesses(db, courtId1);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds processes for the integration-test user (<see cref="TestAuthHandler.TestUserId"/>)
    /// plus an "other user" process to verify ownership isolation, and a soft-deleted one.
    /// </summary>
    private static void SeedProcesses(AppDbContext db, Guid courtId)
    {
        var me = TestAuthHandler.TestUserId;
        var p1 = Guid.NewGuid();

        db.Processes.AddRange(
            // owned, active, unattended → novelty + list
            new Domain.Processes.Process
            {
                Id = p1, UserId = me, FileNumber = "17001400301020240000001", CourtId = courtId,
                CustomAlias = "Cliente Uno", CurrentStatus = "Fijacion estado",
                LastCourtActionAt = new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero),
                Attended = false, IsActive = true, SyncStatus = "ok", SyncPhase = "idle",
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            // owned, active, unattended → novelty + list
            new Domain.Processes.Process
            {
                Id = Guid.NewGuid(), UserId = me, FileNumber = "17001400301020240000002", CourtId = courtId,
                CurrentStatus = "Auto", LastCourtActionAt = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                Attended = false, IsActive = true, SyncStatus = "ok", SyncPhase = "idle",
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            // owned, active, attended → list only (not a novelty)
            new Domain.Processes.Process
            {
                Id = Guid.NewGuid(), UserId = me, FileNumber = "17001400301020240000003", CourtId = courtId,
                CurrentStatus = "Sentencia", LastCourtActionAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
                Attended = true, IsActive = true, SyncStatus = "partial", SyncPhase = "pending_partial_completion",
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            // owned but soft-deleted → must be excluded everywhere
            new Domain.Processes.Process
            {
                Id = Guid.NewGuid(), UserId = me, FileNumber = "17001400301020240000004", CourtId = courtId,
                Attended = false, IsActive = false, SyncStatus = "ok", SyncPhase = "idle",
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            },
            // different user → must never appear for the test user
            new Domain.Processes.Process
            {
                Id = Guid.NewGuid(), UserId = "other-user", FileNumber = "17001400301020240000005", CourtId = courtId,
                Attended = false, IsActive = true, SyncStatus = "ok", SyncPhase = "idle",
                CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            }
        );

        db.ProcessSubjects.AddRange(
            new Domain.Processes.ProcessSubject
            {
                Id = Guid.NewGuid(), ProcessId = p1, SubjectType = "Demandante",
                Name = "OSCAR ARTURO ORTIZ HENAO", Source = "api", CreatedAt = DateTimeOffset.UtcNow
            },
            new Domain.Processes.ProcessSubject
            {
                Id = Guid.NewGuid(), ProcessId = p1, SubjectType = "Demandado",
                Name = "FRANCISCA HELENA GONZALEZ ARIAS", Source = "api", CreatedAt = DateTimeOffset.UtcNow
            }
        );

        db.ProcessActions.Add(new Domain.Processes.ProcessAction
        {
            Id = Guid.NewGuid(), ProcessId = p1, ExternalActionId = 1001, ConsecutiveNumber = 82,
            ActionDate = new DateOnly(2026, 3, 20), Action = "Fijacion estado",
            Annotation = "Actuación registrada", RecordedAt = new DateOnly(2026, 3, 20),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}

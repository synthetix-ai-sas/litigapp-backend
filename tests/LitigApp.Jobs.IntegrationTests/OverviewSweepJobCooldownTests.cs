using FluentAssertions;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Processes;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Persistence.Repositories;
using LitigApp.Infrastructure.Sync;
using LitigApp.Jobs;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

/// <summary>
/// PR2 behavior: WAF cooldown gate at start + 403 → set waf_blocked_until and abort the run.
/// </summary>
public sealed class OverviewSweepJobCooldownTests : IAsyncLifetime
{
    private static readonly DateTime FixedNow = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private AppDbContext _db = null!;
    private readonly FakeClock _clock = new() { UtcNow = FixedNow };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task RunAsync_WhenCooldownActive_SkipsRunWithoutCallingApi()
    {
        // Arrange — cooldown deadline 20 min in the future.
        var syncState = new SyncStateService(_db, _clock);
        await syncState.SetWafBlockedUntilAsync(
            new DateTimeOffset(FixedNow, TimeSpan.Zero).AddMinutes(20), "prior 403", CancellationToken.None);

        var process = EligibleProcess("11001310300120230009001");
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);

        var client = new CountingRamaClient(RamaResult<OverviewData?>.Ok(null));
        var job = BuildJob(client);

        // Act
        await job.RunAsync();

        // Assert — gate short-circuited: no API call, process untouched.
        client.CallCount.Should().Be(0);

        var untouched = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id);
        untouched.LastSyncAttemptAt.Should().BeNull();
        untouched.SyncPhase.Should().Be("idle");
    }

    [Fact]
    public async Task RunAsync_WhenWafBlocked_SetsCooldownAndAbortsRun()
    {
        // Arrange — two eligible processes; the client 403s on the first call.
        var p1 = EligibleProcess("11001310300120230009002");
        var p2 = EligibleProcess("11001310300120230009003");
        _db.Processes.AddRange(p1, p2);
        await _db.SaveChangesAsync(CancellationToken.None);

        var client = new CountingRamaClient(
            RamaResult<OverviewData?>.Fail(FailureKind.WafBlocked, "WAF 403 Forbidden"));
        var job = BuildJob(client);

        // Act
        await job.RunAsync();

        // Assert — aborted after the first call.
        client.CallCount.Should().Be(1);

        // waf_blocked_until set to now + 20 min (default WafOptions.CooldownMinutesOnBlock).
        var syncState = new SyncStateService(_db, _clock);
        var blockedUntil = await syncState.GetWafBlockedUntilAsync(CancellationToken.None);
        blockedUntil.Should().NotBeNull();
        blockedUntil!.Value.Should().BeCloseTo(
            new DateTimeOffset(FixedNow, TimeSpan.Zero).AddMinutes(20), TimeSpan.FromSeconds(2));

        // At least one process was never attempted (run aborted).
        var attempted = await _db.Processes.AsNoTracking().CountAsync(p => p.LastSyncAttemptAt != null);
        attempted.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_WhenCooldownExpired_RunsNormally()
    {
        // Arrange — cooldown deadline already in the past.
        var syncState = new SyncStateService(_db, _clock);
        await syncState.SetWafBlockedUntilAsync(
            new DateTimeOffset(FixedNow, TimeSpan.Zero).AddMinutes(-1), "expired", CancellationToken.None);

        var process = EligibleProcess("11001310300120230009004");
        _db.Processes.Add(process);
        await _db.SaveChangesAsync(CancellationToken.None);

        var client = new CountingRamaClient(RamaResult<OverviewData?>.Ok(null)); // not found → idle
        var job = BuildJob(client);

        // Act
        await job.RunAsync();

        // Assert — gate did not skip: process attempted.
        client.CallCount.Should().Be(1);

        var updated = await _db.Processes.AsNoTracking().FirstAsync(p => p.Id == process.Id);
        updated.LastSyncAttemptAt.Should().NotBeNull();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static Process EligibleProcess(string fileNumber) => new()
    {
        Id = Guid.NewGuid(),
        UserId = "user-waf",
        FileNumber = fileNumber,
        SyncPhase = "idle",
        SyncStatus = "ok",
        IsActive = true,
        LastSyncedAt = DateTimeOffset.UtcNow.AddHours(-25), // older than 22h threshold
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
        UpdatedAt = DateTimeOffset.UtcNow.AddDays(-10),
    };

    private OverviewSweepJob BuildJob(IRamaJudicialClient client) => new(
        new ProcessRepository(_db),
        client,
        new SyncStateService(_db, _clock),
        Options.Create(new SweepOptions { BatchSize = 100, MinimumHoursBetweenSyncsPerProcess = 22 }),
        Options.Create(new WafOptions()),
        _clock,
        NullLogger<OverviewSweepJob>.Instance);

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; }
    }

    private sealed class CountingRamaClient(RamaResult<OverviewData?> overviewResult) : IRamaJudicialClient
    {
        public int CallCount { get; private set; }

        public Task<RamaResult<OverviewData?>> GetOverviewByFileNumberAsync(string fileNumber, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(overviewResult);
        }

        public Task<RamaResult<ProcessDetailData?>> GetDetailAsync(long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<RamaResult<List<SubjectData>>> GetSubjectsAsync(long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<RamaResult<List<ActionData>>> GetFirstPageActionsAsync(long externalProcessId, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}

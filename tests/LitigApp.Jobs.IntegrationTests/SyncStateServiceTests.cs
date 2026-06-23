using FluentAssertions;
using LitigApp.Infrastructure.Persistence;
using LitigApp.Infrastructure.Sync;
using LitigApp.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LitigApp.Jobs.IntegrationTests;

public sealed class SyncStateServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private AppDbContext _db = null!;

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

    private SyncStateService BuildService() => new(_db, new SystemDateTimeProvider());

    [Fact]
    public async Task SeedMigration_InsertsThreeSyncStateRows()
    {
        var keys = await _db.SyncStates.AsNoTracking().Select(s => s.Key).ToListAsync();

        keys.Should().Contain(
        [
            "waf_blocked_until",
            "current_overview_throttle_seconds",
            "current_actions_throttle_seconds",
        ]);
    }

    [Fact]
    public async Task GetWafBlockedUntil_WhenSeeded_ReturnsNull()
    {
        var svc = BuildService();

        var result = await svc.GetWafBlockedUntilAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetWafBlockedUntil_ThenGet_ReturnsValueAndPersistsReason()
    {
        var svc = BuildService();
        var until = DateTimeOffset.UtcNow.AddMinutes(20);
        var ct = CancellationToken.None;

        await svc.SetWafBlockedUntilAsync(until, "WAF 403 on overview", ct);

        var result = await svc.GetWafBlockedUntilAsync(ct);
        result.Should().NotBeNull();
        result!.Value.Should().BeCloseTo(until, TimeSpan.FromSeconds(1));

        var row = await _db.SyncStates.AsNoTracking().FirstAsync(s => s.Key == "waf_blocked_until", ct);
        row.Reason.Should().Be("WAF 403 on overview");
    }

    [Fact]
    public async Task GetOverviewThrottleSeconds_WhenSeeded_ReturnsThree()
    {
        var svc = BuildService();

        var result = await svc.GetOverviewThrottleSecondsAsync(CancellationToken.None);

        result.Should().Be(3);
    }

    [Fact]
    public async Task GetActionsThrottleSeconds_WhenSeeded_ReturnsThree()
    {
        var svc = BuildService();

        var result = await svc.GetActionsThrottleSecondsAsync(CancellationToken.None);

        result.Should().Be(3);
    }

    [Fact]
    public async Task SetOverviewThrottleSeconds_ThenGet_ReturnsNewValue()
    {
        var svc = BuildService();
        var ct = CancellationToken.None;

        await svc.SetOverviewThrottleSecondsAsync(5, ct);

        (await svc.GetOverviewThrottleSecondsAsync(ct)).Should().Be(5);
    }

    [Fact]
    public async Task SetOverviewThrottleSeconds_CalledTwice_UpdatesSameRowNoDuplicate()
    {
        var svc = BuildService();
        var ct = CancellationToken.None;

        await svc.SetOverviewThrottleSecondsAsync(5, ct);
        await svc.SetOverviewThrottleSecondsAsync(7, ct);

        (await svc.GetOverviewThrottleSecondsAsync(ct)).Should().Be(7);

        var count = await _db.SyncStates.AsNoTracking()
            .CountAsync(s => s.Key == "current_overview_throttle_seconds", ct);
        count.Should().Be(1);
    }
}

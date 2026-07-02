using System.Globalization;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Domain.Common;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Sync;

/// <summary>
/// Reads/writes the key-value <c>sync_state</c> table (seeded by the SeedSyncState migration).
/// Rows are mutated at runtime by the sweep jobs: WAF cooldown + adaptive throttle.
/// </summary>
internal sealed class SyncStateService(AppDbContext db, IDateTimeProvider clock) : ISyncStateService
{
    private const string WafBlockedUntilKey = "waf_blocked_until";
    private const string OverviewThrottleKey = "current_overview_throttle_seconds";
    private const string ActionsThrottleKey = "current_actions_throttle_seconds";

    /// <summary>Conservative fallback if a throttle row is missing (seed guarantees it exists).</summary>
    private const int DefaultThrottleSeconds = 3;

    public async Task<DateTimeOffset?> GetWafBlockedUntilAsync(CancellationToken ct)
    {
        var row = await db.SyncStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == WafBlockedUntilKey, ct);
        return row?.ValueTimestamp;
    }

    public async Task SetWafBlockedUntilAsync(DateTimeOffset until, string reason, CancellationToken ct)
    {
        var row = await Upsert(WafBlockedUntilKey, ct);

        // Never shorten an active cooldown — concurrent 403 writers (overview + actions)
        // must not let an earlier deadline overwrite a later one.
        if (row.ValueTimestamp is { } current && current >= until)
            return;

        row.ValueTimestamp = until;
        row.Reason = reason;
        row.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public Task<int> GetOverviewThrottleSecondsAsync(CancellationToken ct) =>
        GetThrottleAsync(OverviewThrottleKey, ct);

    public Task SetOverviewThrottleSecondsAsync(int seconds, CancellationToken ct) =>
        SetThrottleAsync(OverviewThrottleKey, seconds, ct);

    public Task<int> GetActionsThrottleSecondsAsync(CancellationToken ct) =>
        GetThrottleAsync(ActionsThrottleKey, ct);

    private async Task<int> GetThrottleAsync(string key, CancellationToken ct)
    {
        var row = await db.SyncStates.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, ct);
        return int.TryParse(row?.ValueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : DefaultThrottleSeconds;
    }

    private async Task SetThrottleAsync(string key, int seconds, CancellationToken ct)
    {
        var row = await Upsert(key, ct);
        row.ValueText = seconds.ToString(CultureInfo.InvariantCulture);
        row.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Loads the tracked row for <paramref name="key"/>, creating it if absent.</summary>
    private async Task<SyncState> Upsert(string key, CancellationToken ct)
    {
        var row = await db.SyncStates.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null)
        {
            row = new SyncState { Key = key };
            db.SyncStates.Add(row);
        }

        return row;
    }
}

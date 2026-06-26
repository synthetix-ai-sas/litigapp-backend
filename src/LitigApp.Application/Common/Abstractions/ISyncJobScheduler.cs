namespace LitigApp.Application.Common.Abstractions;

/// <summary>
/// Enqueues/schedules sync-engine background jobs. Abstracts Hangfire so the jobs stay
/// testable (the orchestration can be verified without a real job storage).
/// </summary>
public interface ISyncJobScheduler
{
    /// <summary>Triggers an ActionsSweep run (e.g. after an OverviewSweep finds changes).</summary>
    void EnqueueActionsSweep();

    /// <summary>Schedules an ActionsSweep run after <paramref name="delay"/> (WAF cooldown resume).</summary>
    void ScheduleActionsSweep(TimeSpan delay);

    /// <summary>Triggers the (aggregated) notification dispatch for a single user.</summary>
    void EnqueueUserNotifications(string userId);

    /// <summary>Triggers completion of a process left partial by synchronous creation.</summary>
    void EnqueuePartialFetch(Guid processId);

    /// <summary>Schedules a partial-fetch completion after <paramref name="delay"/> (WAF cooldown resume).</summary>
    void SchedulePartialFetch(Guid processId, TimeSpan delay);
}

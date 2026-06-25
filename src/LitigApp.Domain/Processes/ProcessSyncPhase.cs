namespace LitigApp.Domain.Processes;

/// <summary>
/// Canonical values for <see cref="Process.SyncPhase"/>. Used across the sync engine
/// (jobs, repository, creation flow) — change a phase string here, nowhere else.
/// </summary>
public static class ProcessSyncPhase
{
    /// <summary>Up to date; waiting for the next cycle.</summary>
    public const string Idle = "idle";

    /// <summary>Needs an overview check on the next OverviewSweep.</summary>
    public const string PendingOverview = "pending_overview";

    /// <summary>Overview detected a change; needs an actions fetch.</summary>
    public const string PendingActions = "pending_actions";

    /// <summary>Freshly created; needs the full initial fetch (detail + subjects + actions).</summary>
    public const string PendingInitialFull = "pending_initial_full";

    /// <summary>Creation left partial (403/error); completed later by CompletePartialFetchJob.</summary>
    public const string PendingPartialCompletion = "pending_partial_completion";
}

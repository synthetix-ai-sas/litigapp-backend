namespace LitigApp.Api.Hangfire;

public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>Basic Auth password for /hangfire outside Development. Null/empty denies access.</summary>
    public string? DashboardPassword { get; init; }
}

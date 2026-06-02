namespace LitigApp.Domain.Processes;

public class ProcessSubject
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public long? ExternalSubjectId { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public bool IsSummoned { get; set; }
    public string? Identification { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>'api' | 'manual'</summary>
    public string Source { get; set; } = "api";

    /// <summary>Raw JSON payload from the API (jsonb).</summary>
    public string? RawPayload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Process? Process { get; set; }
}

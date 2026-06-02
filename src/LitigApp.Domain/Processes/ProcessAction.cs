namespace LitigApp.Domain.Processes;

public class ProcessAction
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public long ExternalActionId { get; set; }
    public int ConsecutiveNumber { get; set; }
    public DateOnly? ActionDate { get; set; }
    public string? Action { get; set; }
    public string? Annotation { get; set; }
    public DateOnly? TermStartDate { get; set; }
    public DateOnly? TermEndDate { get; set; }
    public DateOnly? RecordedAt { get; set; }
    public bool HasDocuments { get; set; }
    public string? RuleCode { get; set; }

    /// <summary>Self-reference: if this is a "Fijación" pairing an "Auto", points to the Auto row.</summary>
    public Guid? GroupedWithId { get; set; }

    /// <summary>Raw JSON payload from the API (jsonb).</summary>
    public string? RawPayload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Process? Process { get; set; }
    public ProcessAction? GroupedWith { get; set; }
}

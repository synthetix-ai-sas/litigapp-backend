namespace LitigApp.Domain.Imports;

public class ImportJob
{
    public Guid Id { get; set; }

    /// <summary>FK to AspNetUsers.Id (text).</summary>
    public string UserId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }

    /// <summary>'pending' | 'running' | 'paused' | 'completed' | 'failed'</summary>
    public string Status { get; set; } = "pending";

    /// <summary>Column mapping confirmed by user (jsonb).</summary>
    public string? ColumnMapping { get; set; }

    /// <summary>Per-row errors array (jsonb): [{row, radicado, code, message}, ...]</summary>
    public string? Errors { get; set; }

    /// <summary>Job-level failure reason (e.g. "preview_expired").</summary>
    public string? SyncError { get; set; }

    /// <summary>Correlation id of the preview this job came from (traceability only).</summary>
    public Guid PreviewId { get; set; }

    /// <summary>
    /// The confirmed import rows reduced to the mapped columns, as JSON
    /// (<c>[{ "radicado": ..., "notes": ... }]</c>). Persisted here so the worker
    /// process — which does not share the API's in-memory preview cache — can run
    /// the import, and so it survives WAF pause/resume. See spec A1 (4.C).
    /// </summary>
    public string? PreviewPayload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

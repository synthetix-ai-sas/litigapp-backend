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

    /// <summary>Per-row errors array (jsonb): [{row, message}, ...]</summary>
    public string? Errors { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

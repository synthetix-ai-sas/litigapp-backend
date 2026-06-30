using System.ComponentModel.DataAnnotations;

namespace LitigApp.Application.Features.Imports;

/// <summary>
/// Hard anti-OOM limits for Excel import (blueprint §9 Step 9). Bound from section "Import".
/// </summary>
public sealed class ImportOptions
{
    public const string SectionName = "Import";

    /// <summary>Max upload size in bytes (default 2 MB). Exceeding it → 413.</summary>
    [Range(1024, 50 * 1024 * 1024)]
    public long MaxFileSizeBytes { get; init; } = 2 * 1024 * 1024;

    /// <summary>Max data rows per import (default 5000). Exceeding it → 422 TOO_MANY_ROWS.</summary>
    [Range(1, 100_000)]
    public int MaxRows { get; init; } = 5000;

    /// <summary>How long a parsed preview stays cached awaiting the execute call.</summary>
    [Range(1, 120)]
    public int PreviewCacheTtlMinutes { get; init; } = 10;
}

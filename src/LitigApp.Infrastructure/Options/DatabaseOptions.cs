using System.ComponentModel.DataAnnotations;

namespace LitigApp.Infrastructure.Options;

/// <summary>Database connection pool settings. Bound from section "Database".</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Npgsql connection pool ceiling. Keep api + worker total under the plan's connection limit
    /// (Supabase free-tier session pooler = 15). Null → Npgsql default (100).
    /// </summary>
    [Range(1, 100)]
    public int? MaxPoolSize { get; init; }
}

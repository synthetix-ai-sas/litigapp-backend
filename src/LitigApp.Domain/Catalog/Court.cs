namespace LitigApp.Domain.Catalog;

public class Court
{
    public Guid Id { get; set; }

    /// <summary>codDespachoCompleto from the API — 12 chars, unique.</summary>
    public string OfficialCode { get; set; } = string.Empty;

    /// <summary>FK to City.Id — DANE 5-char code (char(5)).</summary>
    public string CityId { get; set; } = string.Empty;
    /// <summary>FK to Entity.Code — DANE 2-char code (char(2)). Nullable until validated against catalog.</summary>
    public string? EntityCode { get; set; }
    /// <summary>FK to Specialty.Code — DANE 2-char code (char(2)). Nullable until validated against catalog.</summary>
    public string? SpecialtyCode { get; set; }
    public short? CourtNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Raw JSON payload from the API (jsonb).</summary>
    public string? RawPayload { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public City? City { get; set; }
    public Entity? JudicialEntity { get; set; }
    public Specialty? Specialty { get; set; }
}

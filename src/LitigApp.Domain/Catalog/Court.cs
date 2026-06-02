namespace LitigApp.Domain.Catalog;

public class Court
{
    public Guid Id { get; set; }

    /// <summary>codDespachoCompleto from the API — 12 chars, unique.</summary>
    public string OfficialCode { get; set; } = string.Empty;

    public int CityId { get; set; }
    public short? EntityId { get; set; }
    public short? SpecialtyId { get; set; }
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

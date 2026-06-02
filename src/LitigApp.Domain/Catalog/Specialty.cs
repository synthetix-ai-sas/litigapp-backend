namespace LitigApp.Domain.Catalog;

/// <summary>
/// Especialidad del despacho (e.g. Id=3, Code="03", Name="CIVIL").
/// </summary>
public class Specialty
{
    public short Id { get; set; }

    /// <summary>Two-character code (char(2)).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Court> Courts { get; set; } = [];
}

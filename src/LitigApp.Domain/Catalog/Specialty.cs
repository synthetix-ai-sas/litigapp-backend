namespace LitigApp.Domain.Catalog;

/// <summary>
/// Especialidad del despacho (e.g. Code="03", Name="CIVIL").
/// </summary>
public class Specialty
{
    /// <summary>Natural primary key — two-character DANE code (char(2), e.g. "03").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Court> Courts { get; set; } = [];
}

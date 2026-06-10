namespace LitigApp.Domain.Catalog;

/// <summary>
/// Entidad judicial (e.g. Code="71", Name="CENTRO DE SERVICIOS JUDICIALES").
/// </summary>
public class Entity
{
    /// <summary>Natural primary key — two-character DANE code (char(2), e.g. "71").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Court> Courts { get; set; } = [];
}

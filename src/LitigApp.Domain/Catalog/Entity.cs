namespace LitigApp.Domain.Catalog;

/// <summary>
/// Entidad judicial (e.g. Id=71, Code="71", Name="CENTRO DE SERVICIOS JUDICIALES").
/// </summary>
public class Entity
{
    public short Id { get; set; }

    /// <summary>Two-character code (char(2)).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<Court> Courts { get; set; } = [];
}

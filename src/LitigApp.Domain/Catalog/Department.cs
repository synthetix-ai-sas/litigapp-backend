namespace LitigApp.Domain.Catalog;

public class Department
{
    /// <summary>DANE 2-char code (natural key, e.g. "17" = Caldas). char(2), preserves leading zeros.</summary>
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<City> Cities { get; set; } = [];
}

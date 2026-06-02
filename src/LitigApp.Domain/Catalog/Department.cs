namespace LitigApp.Domain.Catalog;

public class Department
{
    /// <summary>DANE 2-digit code (e.g. 17 = Caldas).</summary>
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    public ICollection<City> Cities { get; set; } = [];
}

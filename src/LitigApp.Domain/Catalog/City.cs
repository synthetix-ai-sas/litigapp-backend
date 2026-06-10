namespace LitigApp.Domain.Catalog;

public class City
{
    /// <summary>DANE 5-char code (natural key, e.g. "17001" = Manizales). char(5), preserves leading zeros.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>FK to Department.Id — DANE 2-char code (char(2)).</summary>
    public string DepartmentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Navigation
    public Department? Department { get; set; }
    public ICollection<Court> Courts { get; set; } = [];
}

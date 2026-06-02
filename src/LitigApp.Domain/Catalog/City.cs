namespace LitigApp.Domain.Catalog;

public class City
{
    /// <summary>DANE 5-digit code (e.g. 17001 = Manizales).</summary>
    public int Id { get; set; }
    public short DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation
    public Department? Department { get; set; }
    public ICollection<Court> Courts { get; set; } = [];
}

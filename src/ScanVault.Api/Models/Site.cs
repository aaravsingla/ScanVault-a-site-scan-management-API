namespace ScanVault.Api.Models;

/// <summary>
/// A real-world place that gets scanned, e.g. a building floor or a construction site.
/// One site has many scans (one-to-many relationship).
/// </summary>
public class Site : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }

    public List<Scan> Scans { get; set; } = new();
}

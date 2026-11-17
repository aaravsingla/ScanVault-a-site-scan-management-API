namespace ScanVault.Api.Models;

/// <summary>
/// Base class shared by every database entity (inheritance).
/// Each table gets an integer primary key and a creation timestamp for free.
/// </summary>
public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
